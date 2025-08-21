using System.Diagnostics.CodeAnalysis;

namespace RxSockets;

internal sealed class SocketDisposer : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly IAsyncDisposable Disposable;
    private readonly string Name;
    private readonly TaskCompletionSource<bool> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource ReceiveCts;
    private int Disposals; // state
    internal bool DisposeRequested => Disposals > 0;

    internal SocketDisposer(Socket socket, string name, ILogger logger, CancellationTokenSource receiveCts) :
        this(socket, AsyncEmptyDisposable.Instance, name, logger, receiveCts)  { }
    internal SocketDisposer(Socket socket, IAsyncDisposable disposable, string name, ILogger logger, CancellationTokenSource receiveCts)
    {
        Socket = socket;
        ReceiveCts = receiveCts;
        Logger = logger;
        Name = name;
        Disposable = disposable;
    }

    [SuppressMessage("Usage", "CA1031:Catch more specific exception type.")]
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Increment(ref Disposals) > 1)
        {
            await Tcs.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await ReceiveCts.CancelAsync().ConfigureAwait(false);

            EndPoint? localEndPoint = Socket.LocalEndPoint;
            EndPoint? remoteEndPoint = Socket.RemoteEndPoint;

            if (Socket.Connected)
            {
                // disables Send method and queues up a zero-byte send packet in the send buffer
                Socket.Shutdown(SocketShutdown.Send);
                await Socket.DisconnectAsync(false).ConfigureAwait(false);
                Logger.LogDebug("{Name} on {LocalEndPoint} disconnected from {RemoteEndPoint} and disposed.", Name, localEndPoint, remoteEndPoint);
            }
            else
            {
                Logger.LogDebug("{Name} on {LocalEndPoint} disposed.", Name, localEndPoint);
            }

            // SocketAcceptor or AsyncEmptyDisposable
            await Disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "DisposeAsync.");
        }

        try
        {
            Socket.Dispose();
            Tcs.SetResult(true);
        }
        catch (Exception e)
        {
            Logger.LogWarning(e, "DisposeAsync.");
        }
    }
}
