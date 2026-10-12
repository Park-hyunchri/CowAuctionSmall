using System.IO;
using System.IO.Pipes;

namespace GAMSTest.Services;

public sealed class NamedPipeCommandServer : IDisposable
{
    public const string PipeName = "GAMSTest.DisplayCommand";

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;
    private readonly Action<string> _commandHandler;
    private readonly string _pipeName;
    private int _disposed;
    public Task Completion => _serverTask;

    public NamedPipeCommandServer(Action<string> commandHandler, string pipeName = PipeName)
    {
        _commandHandler = commandHandler;
        _pipeName = pipeName;
        _serverTask = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(pipe);
                var command = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(command) && !cancellationToken.IsCancellationRequested)
                    _commandHandler(command.Trim());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                if (!cancellationToken.IsCancellationRequested) await Task.Delay(100);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _cts.Cancel();
        _ = _serverTask.ContinueWith(_ => _cts.Dispose(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}
