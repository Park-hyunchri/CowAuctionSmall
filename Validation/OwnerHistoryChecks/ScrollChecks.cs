using CowAuctionSmall.Presentation.Animation;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.ViewModels;
using CowAuctionSmall.Views.Size128_128.Running;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

static class ScrollChecks
{
    static void Pump(int ms)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    static object? Animation(UserControl host) => host.GetType().GetField("_flowTextAnimation", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host);
    static void Check(bool value, string message) { if (!value) throw new Exception("Scroll: " + message); }
    public static void Run()
    {
        int checks = 0;
        foreach (bool running in new[] { false, true })
        {
            var host = new RunningNoteHost128 { DataContext = new object() };
            var page = host.GetType().GetProperty("PageContent")!;
            var sample = new gValues { TraceNumber = "000000000000001", SipNumber = "1", SpaceIndex = "1", Note = "LONG ORIGINAL NOTE FOR SCROLL CHECK", NoteChangeOwner = "OWNER_A -> OWNER_B TRANSFER HISTORY AT THE END" };
            using var model = new AuctionContPanelViewModel(sample, null!, new VirtualizingStackPanel(), new List<int> { 5 }, 1);
            if (running)
            {
                host.ScrollOwner = model;
                host.SetBinding(RunningNoteHost128.ItemDisplayProperty, new Binding(nameof(model.ItemDisplay)) { Source = model, Mode = BindingMode.OneWay });
            }
            void SetItem(gValues item)
            {
                if (running) model.UpdateCowInfo(item);
                else host.ItemDisplay = new AuctionItemDisplayViewModel(item);
            }
            SetItem(sample);
            page.SetValue(host, new Grid());
            var window = new Window { Content = host, Width = 160, Height = 180, Left = -10000, Top = -10000, ShowActivated = false };
            window.Show(); Pump(200);
            var animation = Animation(host)!;
            var clock = (AnimationClock)typeof(FlowTextAnimation).GetField("_animationClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(animation)!;
            var note = (TextBlock)host.FindName("note");
            // Move the real WPF clock near the trailing history, then change pages repeatedly.
            clock.Controller!.SeekAlignedToLastTick(clock.Timeline.Duration.TimeSpan - TimeSpan.FromSeconds(4), TimeSeekOrigin.BeginTime);
            Pump(80);
            Check(((TranslateTransform)note.RenderTransform).X < -100, "history tail reached"); checks++;
            for (int i = 0; i < 4; i++) { page.SetValue(host, new Grid()); Pump(100); }
            Check(ReferenceEquals(animation, Animation(host)) && ((TranslateTransform)note.RenderTransform).X < -100, "page changes retain scroll clock and tail position"); checks++;
            SetItem(sample.Clone()); Pump(100);
            Check(ReferenceEquals(animation, Animation(host)) && ((TranslateTransform)note.RenderTransform).X < -100, "same animal refresh retains position"); checks++;
            var priceOnly = sample.Clone(); priceOnly.LowestPrice = "9876";
            SetItem(priceOnly); Pump(100);
            Check(ReferenceEquals(animation, Animation(host)) && ReferenceEquals(clock, Clock()), "price-only refresh preserves the running clock"); checks++;
            var recolored = sample.Clone(); recolored.Note = note.Text; recolored.NoteChangeOwner = "";
            SetItem(recolored); Pump(100);
            Check(ReferenceEquals(clock, Clock()) && note.Inlines.OfType<Run>().All(run => run.Foreground == Brushes.Red),
                "same displayed text with changed note/history source updates color without restarting"); checks++;
            var changed = sample.Clone(); changed.NoteChangeOwner = "NEW OWNER HISTORY THAT MUST START AGAIN";
            SetItem(changed); Pump(100);
            Check(note.Text.Contains("NEW OWNER HISTORY") && ((TranslateTransform)note.RenderTransform).X > 0, "changed text restarts"); checks++;
            var sameTextAnimation = Animation(host)!;
            var sameTextClock = (AnimationClock)typeof(FlowTextAnimation).GetField("_animationClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sameTextAnimation)!;
            sameTextClock.Controller!.SeekAlignedToLastTick(sameTextClock.Timeline.Duration.TimeSpan - TimeSpan.FromSeconds(4), TimeSeekOrigin.BeginTime);
            Pump(80);
            Check(((TranslateTransform)note.RenderTransform).X < -100, "same-text identity test first advances current clock"); checks++;
            changed = changed.Clone(); changed.TraceNumber = "000000000000002";
            SetItem(changed); Pump(100);
            Check(!ReferenceEquals(sameTextAnimation, Animation(host)) && ((TranslateTransform)note.RenderTransform).X > 0, "different animal restarts even for equal text"); checks++;
            window.Content = null; Pump(100);
            Check(Animation(host) == null, "unload releases animation"); checks++;
            window.Content = host; Pump(100);
            Check(Animation(host) != null, "reload starts animation"); checks++;
            changed = changed.Clone(); changed.TraceNumber = "000000000000003";
            SetItem(changed);
            window.Content = null; Pump(100);
            Check(Animation(host) == null, "queued initialization cancelled on unload"); checks++;
            window.Content = host; Pump(100);
            var currentClock = Clock();
            var offset = currentClock.Timeline.Duration.TimeSpan - TimeSpan.FromSeconds(4);
            currentClock.Controller!.SeekAlignedToLastTick(offset, TimeSeekOrigin.BeginTime);
            typeof(FlowTextAnimation).GetField("_animationStartUtc", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Animation(host), DateTime.UtcNow - offset);
            Pump(80);
            double beforeDetach = ((TranslateTransform)note.RenderTransform).X;
            window.Content = new Grid(); Pump(80);
            double? savedPosition = model.GetNotePosition("__shared_note__");
            string detachedText = note.Text;
            bool sameStoredText = model.IsSameNoteText("__shared_note__", detachedText);
            window.Content = host; Pump(100);
            Check(!running || ((TranslateTransform)note.RenderTransform).X < -100,
                $"running mode restores saved position after an excluded page: before={beforeDetach}, saved={savedPosition}, after={((TranslateTransform)note.RenderTransform).X}, sameStored={sameStoredText}, sameText={detachedText == note.Text}"); checks++;
            BindingOperations.ClearBinding(host, RunningNoteHost128.ItemDisplayProperty);
            host.ItemDisplay = null; Pump(100);
            Check(Animation(host) == null && note.Text == "" && ((Border)host.FindName("borderPaternityMatch")).Visibility == Visibility.Collapsed,
                "null display clears text, badge and clock"); checks++;
            if (running) host.SetBinding(RunningNoteHost128.ItemDisplayProperty, new Binding(nameof(model.ItemDisplay)) { Source = model, Mode = BindingMode.OneWay });
            else host.ItemDisplay = new AuctionItemDisplayViewModel(changed);
            Pump(100);
            Check(Animation(host) != null && note.Text.Contains("NEW OWNER HISTORY"), "display recovers after null input"); checks++;
            var latest = changed.Clone(); latest.TraceNumber = "000000000000004"; latest.Note = "LATEST NOTE";
            SetItem(latest);
            latest = latest.Clone(); latest.TraceNumber = "000000000000005"; latest.Note = "FINAL NOTE";
            SetItem(latest); Pump(100);
            Check(Animation(host) != null && note.Text.StartsWith("FINAL NOTE") && host.ItemDisplay!.Snapshot.TraceNumber == latest.TraceNumber,
                "queued initialization uses latest rapid replacement"); checks++;
            latest = latest.Clone(); latest.Note = ""; latest.NoteChangeOwner = "";
            SetItem(latest); Pump(100);
            Check(note.Text == "" && !(bool)typeof(FlowTextAnimation).GetField("_isAnimating", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Animation(host))!,
                "clearing both note fields stops scrolling"); checks++;
            window.Close();
            AnimationClock Clock() => (AnimationClock)typeof(FlowTextAnimation).GetField("_animationClock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Animation(host))!;
        }
        Console.WriteLine($"PASS {checks} scroll lifecycle checks; real WPF clocks, synthetic text only");
    }
}
