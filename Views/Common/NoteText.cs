using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace CowAuctionSmall.Views.Common
{
    public static class NoteText
    {
        private static readonly DependencyProperty UpdatePendingProperty = DependencyProperty.RegisterAttached(
            "UpdatePending", typeof(bool), typeof(NoteText), new PropertyMetadata(false));
        public static readonly DependencyProperty NormalProperty = DependencyProperty.RegisterAttached(
            "Normal", typeof(string), typeof(NoteText), new PropertyMetadata("", OnChanged));
        public static readonly DependencyProperty HistoryProperty = DependencyProperty.RegisterAttached(
            "History", typeof(string), typeof(NoteText), new PropertyMetadata("", OnChanged));

        public static string GetNormal(DependencyObject target) => (string)target.GetValue(NormalProperty);
        public static void SetNormal(DependencyObject target, string value) => target.SetValue(NormalProperty, value);
        public static string GetHistory(DependencyObject target) => (string)target.GetValue(HistoryProperty);
        public static void SetHistory(DependencyObject target, string value) => target.SetValue(HistoryProperty, value);

        private static void OnChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is not TextBlock text) return;
            if ((bool)text.GetValue(UpdatePendingProperty)) return;
            text.SetValue(UpdatePendingProperty, true);
            text.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
            {
                text.SetValue(UpdatePendingProperty, false);
                UpdateInlines(text);
            }));
        }

        private static void UpdateInlines(TextBlock text)
        {
            string normal = GetNormal(text) ?? "";
            string history = GetHistory(text) ?? "";
            if (string.IsNullOrWhiteSpace(normal) || normal.Trim() == "-") normal = "";
            if (string.IsNullOrWhiteSpace(history)) history = "";

            // One TextBlock preserves the existing width measurement and marquee animation.
            text.Text = normal.Length == 0 ? history : history.Length == 0 ? normal : normal + " / " + history;
            text.Inlines.Clear();
            if (normal.Length > 0) text.Inlines.Add(new Run(normal) { Foreground = Brushes.Red });
            if (normal.Length > 0 && history.Length > 0) text.Inlines.Add(new Run(" / ") { Foreground = Brushes.Red });
            if (history.Length > 0) text.Inlines.Add(new Run(history) { Foreground = Brushes.White });
        }
    }
}
