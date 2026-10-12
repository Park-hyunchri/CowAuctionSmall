using CommunityToolkit.Mvvm.Messaging;
using CowAuctionSmall.Application.Abstractions;
using CowAuctionSmall.Application.Auction;
using CowAuctionSmall.Application.Messaging;
using CowAuctionSmall.Domain.Auction;
using CowAuctionSmall.Infra.Netty.interfaces;
using CowAuctionSmall.Infra.Netty.models;
using CowAuctionSmall.Infra.Netty.netty.handlers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using System.Collections.Concurrent;

internal static class AuctionDisplayStateChecks
{
    internal static async Task Run()
    {
        CheckConnectionException();
        using var netty = new NettyAsyncMsgProcess(new SilentLog());
        var recipient = new object();
        var received = new ConcurrentQueue<AuctionDisplayStateMessage>();
        WeakReferenceMessenger.Default.Register<AuctionDisplayStateMessage>(recipient, (_, message) => received.Enqueue(message));
        try
        {
            Program.Check(netty.CurrentDisplayState == AuctionDisplayState.Empty, "footer starts with unknown auction method and state");
            await Send(netty, State("8004"));
            Program.Check(netty.CurrentDisplayState == AuctionDisplayState.Empty, "AS cannot infer an auction method before AT");
            await Send(netty, new[] { "AT", "fixture", "20" });
            Program.Check(netty.CurrentDisplayState == new AuctionDisplayState(20, AuctionDisplayPhase.Unknown, null),
                "receiving a method alone does not imply a finished auction");
            await Send(netty, State("8002"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Waiting, "AS8002 displays waiting");
            await Send(netty, State("8003"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Waiting, "price adjustment must not display running");
            await Send(netty, State("8004", "bad"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Waiting, "malformed single item cannot start footer auction state");
            await Send(netty, State("8004", "7"));
            Program.Check(netty.CurrentDisplayState == new AuctionDisplayState(20, AuctionDisplayPhase.Running, 7),
                "single progress exposes the validated entry number");
            int published = received.Count;
            await Send(netty, State("8004", "7"));
            await Send(netty, State("8003", "8"));
            await Send(netty, State("8005", "7"));
            await Send(netty, new[] { "SD", "fixture", "F", "-1" });
            await Send(netty, new[] { "SZ", "fixture", "20261001", "0", "1", "H", "1", "999" });
            Program.Check(received.Count == published && netty.CurrentDisplayState.SipNumber == 7,
                "duplicate, price-only, ambiguous AS8005, SD F and SZ H signals do not invent progress changes");
            await Send(netty, new[] { "AF", "fixture", "6", "24" });
            await Send(netty, State("8006", "6"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Running && netty.CurrentDisplayState.SipNumber == 7,
                "a delayed result for another entry cannot finish the current entry");
            await Send(netty, new[] { "AF", "fixture", "7", "24" });
            Program.Check(netty.CurrentDisplayState == new AuctionDisplayState(20, AuctionDisplayPhase.Finished, 7),
                "matching AF cancellation finishes the single auction footer state");
            foreach (string result in new[] { "22", "23" })
            {
                await Send(netty, State("8004", "7"));
                await Send(netty, new[] { "AF", "fixture", "7", result });
                Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Finished, "matching sold and unsold AF results finish footer state");
            }
            await Send(netty, State("8004", "7"));
            await Send(netty, State("8006", "bad"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Running, "invalid completion entry does not finish active auction");
            await Send(netty, State("8006", "7"));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Finished, "matching AS8006 finishes the single auction");
            await Send(netty, new[] { "AT", "fixture", "10" });
            Program.Check(netty.CurrentDisplayState == new AuctionDisplayState(10, AuctionDisplayPhase.Unknown, null),
                "method switch clears the prior entry and result state");
            await Send(netty, State("8004", ""));
            Program.Check(netty.CurrentDisplayState == new AuctionDisplayState(10, AuctionDisplayPhase.Running, null),
                "batch progress does not require or expose an individual entry");
            await Send(netty, new[] { "AF", "fixture", "7", "24" });
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Running, "single AF cannot stop a batch footer state");
            await Send(netty, State("8006", ""));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Finished, "batch AS8006 finishes without an entry number");
            await Send(netty, State("8001", ""));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Unknown, "refresh does not assert a final or running state");
            await Send(netty, State("8007", ""));
            Program.Check(netty.CurrentDisplayState.Phase == AuctionDisplayPhase.Finished, "round end finishes the auction footer state");
            foreach (string connection in new[] { "2001", "2002", "2003", "disconnected" })
            {
                await Send(netty, new[] { "AT", "fixture", "20" });
                await Send(netty, State("8004", "7"));
                WeakReferenceMessenger.Default.Send(new NettyConnectionResultMessage(connection));
                await Drain(netty);
                Program.Check(netty.CurrentDisplayState == AuctionDisplayState.Empty, "failed, duplicate, unavailable and disconnected sessions clear stale auction state");
                WeakReferenceMessenger.Default.Send(new NettyConnectionResultMessage("2000"));
                await Drain(netty);
                Program.Check(netty.CurrentDisplayState == AuctionDisplayState.Empty, "reconnection success alone does not restore stale auction state");
                await Send(netty, State("8004", "7"));
                Program.Check(netty.CurrentDisplayState == AuctionDisplayState.Empty, "reconnected session needs a fresh AT before displaying a method");
            }
            Program.Check(!netty.Ingress.NeedsResync && received.Last().State == netty.CurrentDisplayState,
                "footer event payload matches consumer state without a hidden handler failure");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(recipient);
            netty.Dispose();
            await netty.Ingress.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static string[] State(string state, string entry = "7") => new[] { "AS", "fixture", entry, "1", "0", "0", state };

    private static void CheckConnectionException()
    {
        var controller = new OfflineController();
        var decoder = new AuctionClientInboundDecoder(controller);
        var observer = new ExceptionObserver();
        var channel = new EmbeddedChannel(new ExceptionForwarder(decoder), observer);
        try
        {
            var error = new InvalidOperationException("offline connection exception");
            channel.Pipeline.FireExceptionCaught(error);
            Program.Check(controller.Exceptions == 1 && controller.Disconnections == 0 && channel.Active,
                "an active channel exception reports failure without inventing a channel disconnect");
            Program.Check(ReferenceEquals(observer.Error, error), "decoder still forwards the original exception to the existing pipeline");
        }
        finally
        {
            WeakReferenceMessenger.Default.UnregisterAll(decoder);
            channel.FinishAndReleaseAll();
        }
    }

    private sealed class ExceptionForwarder(AuctionClientInboundDecoder decoder) : ChannelHandlerAdapter
    {
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => decoder.ExceptionCaught(context, exception);
    }

    private sealed class ExceptionObserver : ChannelHandlerAdapter
    {
        internal Exception? Error { get; private set; }
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => Error = exception;
    }

    private sealed class OfflineController : iNettyControllable
    {
        internal int Exceptions { get; private set; }
        internal int Disconnections { get; private set; }
        public void onActiveChannel(IChannelHandlerContext context) { }
        public void OnResponseConnectionInfo(ResponseConnectionInfo response) { }
        public void OnCurrentAuctionData(string data) { }
        public void onCheckSession(IChannelHandlerContext context, AuctionCheckSession session) { }
        public void onChannelInactive(int port) => Disconnections++;
        public void onConnectionException() => Exceptions++;
    }

    private static async Task Send(NettyAsyncMsgProcess netty, string[] fields)
    {
        WeakReferenceMessenger.Default.Send(new DataStringArrMessage(fields));
        await Drain(netty);
    }

    private static async Task Drain(NettyAsyncMsgProcess netty)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        netty.Ingress.TryPost(() => completion.TrySetResult());
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class SilentLog : IAppLog
    {
        public void LogInfo(string message) { }
        public void LogWarn(string message) { }
        public void LogError(string message) { }
    }
}
