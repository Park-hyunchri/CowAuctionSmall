using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using System.Collections.Concurrent;

internal static class SignalChecks
{
    private const string Sensitive = "SYNTHETIC_PRIVATE_AUTH_PRICE_ID";

    internal static async Task Run()
    {
        var accepted = new List<(string[] Fields, string Diagnostic, string Phrase)>();
        foreach (var item in new[] { ("10", "일괄"), ("20", "단일") })
            accepted.Add((Fields("AT", 2, item.Item1), "AT state=" + item.Item1, item.Item2));
        foreach (var item in new[] { ("8001", "새로고침"), ("8002", "대기"), ("8003", "가격 변경"),
            ("8004", "진행"), ("8005", "상태 변경"), ("8006", "결과 확인"), ("8007", "회차 종료") })
            accepted.Add((Fields("AS", 6, item.Item1), "AS state=" + item.Item1, item.Item2));
        foreach (var item in new[] { ("22", "낙찰"), ("23", "유찰"), ("24", "취소") })
        {
            var fields = Fields("AF", 3, item.Item1);
            fields[2] = "7";
            accepted.Add((fields, "AF state=" + item.Item1, item.Item2));
        }
        foreach (var item in new[] { ("P", "경매대상"), ("Y", "미응찰"), ("N", "경매결과"), ("A", "기본영상"), ("H", "시작·재개") })
            accepted.Add((Fields("SZ", 5, item.Item1), "SZ state=" + item.Item1, item.Item2));
        accepted.Add((new[] { "SD", Sensitive, "F", Sensitive }, "SD state=F", "정지·종료 신호 수신"));
        foreach (string count in new[] { "0", "3", "999" })
            accepted.Add((new[] { "SD", Sensitive, "C", count }, "SD state=C", "카운트다운 신호: " + count));

        foreach (var item in accepted)
        {
            string? text = AuctionSignalSummary.StatusText(item.Fields);
            Program.Check(text != null && text.Contains(item.Phrase, StringComparison.Ordinal)
                && !text.Contains(Sensitive, StringComparison.Ordinal), "allowed signal uses a fixed Korean meaning without raw private fields");
            Program.Check(AuctionSignalSummary.Diagnostic(item.Fields) == item.Diagnostic,
                "diagnostic includes only an allowed protocol code and state");
        }

        var rejected = new List<string[]> { Array.Empty<string>(), new[] { Sensitive, Sensitive }, new[] { "AS" },
            Fields("AT", 2, Sensitive), Fields("AS", 6, Sensitive), Fields("AF", 3, Sensitive), Fields("SZ", 5, Sensitive),
            Fields("SD", 2, Sensitive), new[] { "SV", Sensitive }, new[] { "AR", Sensitive }, new[] { "SS", Sensitive } };
        foreach (string count in new[] { "", "-1", "1000", "1.5", "+1", " 1", "1 ", "2147483648", Sensitive })
            rejected.Add(new[] { "SD", Sensitive, "C", count });
        foreach (var fields in rejected)
        {
            Program.Check(AuctionSignalSummary.StatusText(fields) == null,
                "unknown signals, malformed counters, and raw private strings produce no status text");
            string diagnostic = AuctionSignalSummary.Diagnostic(fields);
            Program.Check(!diagnostic.Contains(Sensitive, StringComparison.Ordinal)
                && (diagnostic is "OTHER" or "AT" or "AS" or "AF" or "SZ" or "SD" or "SV" or "AR" or "SS" or "SD state=C"),
                "malformed input diagnostics are restricted to known code labels");
        }

        var log = new CapturedLog();
        using var netty = new NettyAsyncMsgProcess(log);
        var recipient = new object();
        var received = new ConcurrentQueue<AuctionSignalMessage>();
        var displays = new ConcurrentQueue<DataToServerGetArrMsg>();
        WeakReferenceMessenger.Default.Register<AuctionSignalMessage>(recipient, (_, message) => received.Enqueue(message));
        WeakReferenceMessenger.Default.Register<DataToServerGetArrMsg>(recipient, (_, message) => displays.Enqueue(message));
        try
        {
            foreach (var item in accepted)
            {
                int before = received.Count;
                DateTime started = DateTime.Now;
                await Send(netty, item.Fields);
                Program.Check(received.Count == before + 1 && received.Last().Text.Contains(item.Phrase, StringComparison.Ordinal)
                    && received.Last().ReceivedAt >= started && received.Last().ReceivedAt <= DateTime.Now,
                    "the real protocol consumer publishes a timestamped allowed signal summary");
            }
            foreach (var fields in rejected)
            {
                int before = received.Count;
                await Send(netty, fields);
                Program.Check(received.Count == before, "the real consumer does not publish malformed or unknown signal text");
            }

            foreach (string mode in new[] { "20", "10" })
            {
                await Send(netty, new[] { "AT", "fixture", mode });
                ServerGetData._runRunSipNumber = 7;
                ServerGetData._batchRunningState = true;
                long generation = netty.Ingress.Generation;
                await Send(netty, new[] { "SD", Sensitive, "F", "-1" });
                Program.Check(received.Last().Text == "정지·종료 신호 수신" && ServerGetData._runRunSipNumber == 7
                    && ServerGetData._batchRunningState && netty.Ingress.Generation == generation,
                    "SD F reports receipt without changing running state or invalidating requests in either mode");
            }

            foreach (string kind in new[] { "A", "H" })
            {
                long generation = netty.Ingress.Generation;
                int displayCount = displays.Count;
                await Send(netty, Fields("SZ", 5, kind));
                Program.Check(netty.Ingress.Generation == generation && displays.Count == displayCount,
                    "SZ A and H show receipt information without starting a data refresh or invalidating active requests");
            }
            foreach (string kind in new[] { "P", "Y", "N" })
            {
                long generation = netty.Ingress.Generation;
                int displayCount = displays.Count;
                await Send(netty, Fields("SZ", 5, kind));
                Program.Check(netty.Ingress.Generation > generation && displays.Count == displayCount + 1,
                    "SZ P Y and N retain their existing data refresh message flow");
            }
            Program.Check(received.All(message => !message.Text.Contains(Sensitive, StringComparison.Ordinal))
                && log.Messages.All(message => !message.Contains(Sensitive, StringComparison.Ordinal)),
                "signal publications and protocol logs do not expose private marker fields");
            Program.Check(!netty.Ingress.NeedsResync, "signal-only checks complete without hidden handler failures");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
            netty.Dispose();
            await netty.Ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
            ServerGetData._runRunSipNumber = -1;
            ServerGetData._batchRunningState = false;
        }
    }

    private static string[] Fields(string code, int statusIndex, string status)
    {
        var fields = Enumerable.Repeat(Sensitive, 8).ToArray();
        fields[0] = code;
        fields[statusIndex] = status;
        return fields;
    }

    private static async Task Send(NettyAsyncMsgProcess netty, string[] fields)
    {
        WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
        var consumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        netty.Ingress.TryPost(() => consumed.TrySetResult());
        await consumed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class CapturedLog : IAppLog
    {
        internal ConcurrentQueue<string> Messages { get; } = new();
        public void LogInfo(string message) => Messages.Enqueue(message);
        public void LogWarn(string message) => Messages.Enqueue(message);
        public void LogError(string message) => Messages.Enqueue(message);
    }
}
