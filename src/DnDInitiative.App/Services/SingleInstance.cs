using System.IO;
using System.IO.Pipes;

namespace DnDInitiative.App.Services;

/// <summary>
/// Detects whether another copy of the app is running, and passes messages to it.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const int ConnectTimeoutMilliseconds = 2000;

    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _stopListening = new();

    public SingleInstance(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        _pipeName = applicationId + ".pipe";
        _mutex = new Mutex(initiallyOwned: false, @"Local\" + applicationId, out bool createdNew);
        IsFirstInstance = createdNew;
    }

    public bool IsFirstInstance { get; }

    /// <summary>Called by a second copy. Returns false if the first copy did not answer.</summary>
    public async Task<bool> SendToFirstInstanceAsync(string message)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.CurrentUserOnly);
            await client.ConnectAsync(ConnectTimeoutMilliseconds);

            using var writer = new StreamWriter(client) { AutoFlush = true };
            await writer.WriteLineAsync(message);
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Called by the first copy. Runs until disposed, passing each received message to the handler.
    /// </summary>
    public async Task ListenAsync(Func<string, Task> onMessage)
    {
        ArgumentNullException.ThrowIfNull(onMessage);

        CancellationToken cancellationToken = _stopListening.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

                await server.WaitForConnectionAsync(cancellationToken);

                string? message;
                try
                {
                    using var reader = new StreamReader(server);
                    message = await reader.ReadLineAsync(cancellationToken);
                }
                catch (IOException)
                {
                    // The other side went away mid-message. Wait for the next one.
                    continue;
                }

                if (message is not null)
                {
                    await onMessage(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal: the app is closing.
        }
    }

    public void Dispose()
    {
        _stopListening.Cancel();
        _stopListening.Dispose();
        _mutex.Dispose();
    }
}