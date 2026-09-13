using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace GAMSTest.Services;

public sealed class NamedPipeCommandServer : IDisposable
{
    public const string PipeName = "GAMSTest.DisplayCommand";

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _serverTask;
    private readonly Action<string> _commandHandler;

    public NamedPipeCommandServer(Action<string> commandHandler)
    {
        _commandHandler = commandHandler;
        _serverTask = RunAsync(_cts.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipe.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(pipe);
                var command = await reader.ReadLineAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(command)) _commandHandler(command.Trim());
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                if (!cancellationToken.IsCancellationRequested) await Task.Yield();
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
