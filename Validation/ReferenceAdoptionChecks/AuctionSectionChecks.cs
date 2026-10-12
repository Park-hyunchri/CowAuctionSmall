using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Infra.Api;
using System.Net;
using System.Net.Http;
using CowAuctionSmall.ViewModels;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

internal static class AuctionSectionChecks
{
    internal static async Task Run()
    {
        CheckSectionDisplay();
        foreach (string kind in new[] { "A", "P", "Y", "N", "H" })
        foreach (int cow in Enumerable.Range(0, 4))
        {
            var fields = Sz(kind, cow);
            var state = AuctionSectionState.FromSignal(fields);
            Program.Check(state.IsRangeConfirmed && state.CowType == cow && state.SectionNumber == 2 &&
                state.AuctionDate == new DateOnly(2026, 9, 30) && state.DisplayCommand == kind &&
                state.Range == new AuctionSectionRange(201, 999), "SZ retains typed selection and inclusive range");
            fields[6] = "500";
            Program.Check(state.Range!.StartNumber == 201, "section state does not retain the mutable protocol array");
        }
        foreach (var change in new[] { (2, "20260230"), (3, "4"), (4, "-1"), (5, "?"),
            (6, "0"), (6, "1000"), (7, ""), (7, "2147483648"), (6, "+1"), (6, " 1"), (1, "fixture") })
        {
            var fields = Sz(); fields[change.Item1] = change.Item2;
            var state = AuctionSectionState.FromSignal(fields);
            Program.Check(!state.IsValid && state.Range == null && state.CowType == null, "malformed SZ cannot confirm or retain a range");
        }
        var large = Sz(); large[6] = "1000"; large[7] = "2000";
        Program.Check(AuctionSectionState.FromSignal(large).IsRangeConfirmed, "999 is not a hard-coded maximum");
        var asState = AuctionSectionState.FromSignal(As().Append("").ToArray());
        Program.Check(asState.IsValid && !asState.IsRangeConfirmed && asState.SectionNumber == 2 &&
            asState.CowType == 1 && asState.AuctionDate == null && asState.DisplayCommand == null,
            "AS fixed field positions survive trailing delimiters without inventing dates or ranges");
        var noSection = As(); noSection[12] = "0";
        Program.Check(AuctionSectionState.FromSignal(noSection).SectionNumber == 0 &&
            !AuctionSectionState.FromSignal(noSection).IsRangeConfirmed, "AS zero section remains unconfirmed");
        foreach (var fields in new[] { Array.Empty<string>(), new[] { "SZ" }, As().Take(12).ToArray() })
            Program.Check(!AuctionSectionState.FromSignal(fields).IsValid, "incomplete frames have no valid section");

        using var netty = new NettyAsyncMsgProcess(new NullLog());
        var recipient = new object();
        var messages = new List<DataToServerGetArrMsg>();
        var sections = new List<AuctionSectionMessage>();
        WeakReferenceMessenger.Default.Register<DataToServerGetArrMsg>(recipient, (_, msg) => messages.Add(msg));
        WeakReferenceMessenger.Default.Register<AuctionSectionMessage>(recipient, (_, msg) => sections.Add(msg));
        try
        {
            await Send(netty, new[] { "AT", "123", "10" });
            int count = messages.Count;
            await Send(netty, Sz("A"));
            Program.Check(netty.CurrentSection.IsRangeConfirmed && messages.Count == count,
                "SZ A stores metadata without creating a screen refresh");
            Program.Check(sections.Last().State == netty.CurrentSection, "section UI message carries the current immutable snapshot");
            int sectionCount = sections.Count;
            await Send(netty, Sz("A"));
            Program.Check(sections.Count == sectionCount, "identical section signals do not republish UI metadata");
            await Send(netty, Sz("P"));
            Program.Check(netty.CurrentSection.IsRangeConfirmed && messages.Count == count + 1 &&
                messages.Last().Data.SequenceEqual(new[] { "10", "SZ", "8000", "20260930" }),
                "SZ P retains the existing full-list refresh message");
            await Send(netty, As());
            Program.Check(netty.CurrentSection.IsValid && !netty.CurrentSection.IsRangeConfirmed &&
                messages.Last().Data.SequenceEqual(new[] { "10", "AS", "8002" }),
                "AS replaces old SZ range with unresolved metadata without altering business messages");
            await Send(netty, Sz());
            await Send(netty, As().Take(12).ToArray());
            Program.Check(!netty.CurrentSection.IsValid && netty.CurrentSection.Range == null,
                "missing AS selection clears previously confirmed SZ range");
            Program.Check(!sections.Last().State.IsValid, "invalid metadata also clears the section displayed by the UI");
            await Send(netty, Sz());
            await Send(netty, new[] { "AT", "123", "20" });
            await Send(netty, As());
            Program.Check(netty.CurrentSection == AuctionSectionState.Empty, "switching to single auction clears batch metadata");
            Program.Check(sections.Last().State == AuctionSectionState.Empty, "single auction removes the displayed batch suffix");
            Program.Check(!netty.Ingress.NeedsResync, "metadata capture produces no consumer failures");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
            netty.Dispose();
            await netty.Ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        }

        var handler = new FakeHttp();
        using var http = new HttpClient(handler);
        var api = new AuctionSectionApiClient(http);
        Task<AuctionSectionRange?> Read(int cow = 0, int section = 2, CancellationToken ct = default) =>
            api.ReadAsync(new Uri("https://fixture.invalid/api/v1/auction/select/qcn"), "fixture-token", "123",
                new DateOnly(2026, 9, 30), cow, section, ct);
        handler.Body = "{\"success\":true,\"stnList\":[{\"RG_SQNO\":1,\"ST_AUC_NO\":1,\"ED_AUC_NO\":200},{\"RG_SQNO\":2,\"ST_AUC_NO\":201,\"ED_AUC_NO\":999}]}";
        Program.Check(await Read() == new AuctionSectionRange(201, 999), "API selects the requested section, not the first section");
        Program.Check(handler.Method == HttpMethod.Post && handler.MediaType == "application/x-www-form-urlencoded" &&
            handler.Form == "naBzplc=123&aucDt=20260930&aucObjDsc=0&aucDsc=2" && handler.HasBearer,
            "section API sends the reference form contract and bearer authentication");
        foreach (string body in new[] { "invalid", "{}", "{\"success\":false,\"stnList\":[]}",
            "{\"success\":true,\"stnList\":[]}",
            "{\"success\":true,\"stnList\":[{\"RG_SQNO\":2,\"ST_AUC_NO\":999,\"ED_AUC_NO\":1}]}",
            "{\"success\":true,\"stnList\":[{\"RG_SQNO\":2,\"ST_AUC_NO\":1.5,\"ED_AUC_NO\":999}]}",
            "{\"success\":true,\"stnList\":[{\"RG_SQNO\":2},{\"RG_SQNO\":2}]}" })
        {
            handler.Body = body;
            Program.Check(await Read() == null, "invalid or ambiguous API response cannot produce a confirmed range");
        }
        handler.Status = HttpStatusCode.Unauthorized;
        Program.Check(await Read() == null, "failed API request does not return a previous range");
        int calls = handler.Calls;
        Program.Check(await Read(section: 0) == null && await Read(cow: 4) == null && handler.Calls == calls,
            "unsupported or unspecified selections do not issue HTTP requests");
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        bool canceled = false;
        try { await Read(ct: cancel.Token); } catch (OperationCanceledException) { canceled = true; }
        Program.Check(canceled, "caller cancellation is preserved");
        await AuctionSectionLookupChecks.Run();
    }

    private static void CheckSectionDisplay()
    {
        var model = (MainWindowViewModel)RuntimeHelpers.GetUninitializedObject(typeof(MainWindowViewModel));
        typeof(MainWindowViewModel).GetField("_dispatcher", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(model, Dispatcher.CurrentDispatcher);
        var method = typeof(MainWindowViewModel).GetMethod("OnAuctionSection", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var notifications = new List<string?>();
        model.PropertyChanged += (_, e) => notifications.Add(e.PropertyName);
        model.AuctionSignalText = "경매 대기 신호 수신";
        void Apply(AuctionSectionState state)
        {
            method.Invoke(model, new object[] { model, new AuctionSectionMessage(state) });
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
        string[] names = { "전체", "송아지", "비육우", "번식우" };
        for (int cow = 0; cow < names.Length; cow++)
        {
            Apply(AuctionSectionState.FromSignal(Sz(cow: cow)));
            Program.Check(model.AuctionSignalDisplayText == $"경매 대기 신호 수신 | 구분 {cow}({names[cow]}) · 2구간 · 경매프로그램 선택 구간 201~999번",
                "status line displays the numeric animal code, label and confirmed range");
        }
        Apply(AuctionSectionState.FromSignal(As()));
        Program.Check(model.AuctionSignalDisplayText.EndsWith("구분 1(송아지) · 2구간 · 범위 미확인"),
            "AS status does not keep the previously confirmed range");
        Apply(AuctionSectionState.FromSignal(new[] { "SZ" }));
        Program.Check(model.AuctionSignalDisplayText == "경매 대기 신호 수신 | 구간 정보 미확인", "invalid input removes stale range text");
        Apply(AuctionSectionState.Empty);
        Program.Check(model.AuctionSignalDisplayText == "경매 대기 신호 수신", "mode reset restores the plain signal status");
        Program.Check(notifications.Count(name => name == nameof(MainWindowViewModel.AuctionSignalDisplayText)) >= 8,
            "both signal and section changes notify the displayed binding");
    }

    private static string[] Sz(string command = "P", int cow = 0) =>
        new[] { "SZ", "123", "20260930", cow.ToString(), "2", command, "201", "999" };
    private static string[] As() => new[] { "AS", "123", "", "1", "", "", "8002", "", "", "", "", "", "2", "1" };
    private static async Task Send(NettyAsyncMsgProcess netty, string[] fields)
    {
        WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        netty.Ingress.TryPost(() => done.TrySetResult());
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    private sealed class NullLog : CowAuctionSmall.Application.Abstractions.IAppLog
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
    private sealed class FakeHttp : HttpMessageHandler
    {
        internal string Body = "{}";
        internal HttpStatusCode Status = HttpStatusCode.OK;
        internal string? Form, MediaType;
        internal HttpMethod? Method;
        internal bool HasBearer;
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Method = request.Method; MediaType = request.Content?.Headers.ContentType?.MediaType;
            HasBearer = request.Headers.Authorization?.Scheme == "Bearer";
            Form = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status) { Content = new StringContent(Body) };
        }
    }
}
