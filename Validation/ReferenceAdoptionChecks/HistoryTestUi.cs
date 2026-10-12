using System.Collections.Concurrent;
using System.Diagnostics;

internal static class HistoryTestUi
{
    internal static void Flush(ConcurrentQueue<Action> queue)
    {
        while (queue.TryDequeue(out var action)) action();
    }

    internal static async Task Until(Func<bool> condition, ConcurrentQueue<Action>? queue = null)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (queue != null) Flush(queue);
            if (timer.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Expected history work or UI publication.");
            await Task.Delay(1);
        }
    }

    internal static async Task Complete(Task completion, ConcurrentQueue<Action> queue)
    {
        await Until(() => completion.IsCompleted, queue);
        await completion;
        Flush(queue);
    }

    internal static async Task<T> Await<T>(Task<T> task, ConcurrentQueue<Action> queue)
    {
        await Complete(task, queue);
        return await task;
    }
}
