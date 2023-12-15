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

    internal SocketDisposer(Socket socket, string name, CancellationTokenSource receiveCts, ILogger logger) :
        this(socket, AsyncEmptyDisposable.Instance, name, receiveCts, logger) { }
    internal SocketDisposer(Socket socket, IAsyncDisposable disposable, string name, CancellationTokenSource receiveCts, ILogger logger)
    {
        Socket = socket;
        ReceiveCts = receiveCts;
        Logger = logger;
        Name = name;
        Disposable = disposable;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Increment(ref Disposals) > 1)
        {
            await Tcs.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            ReceiveCts.Cancel();

            EndPoint? localEndPoint = Socket.LocalEndPoint;
            EndPoint? remoteEndPoint = Socket.RemoteEndPoint;

            if (Socket.Connected)
            {
                // disables Send method and queues up a zero-byte send packet in the send buffer
                Socket.Shutdown(SocketShutdown.Send);
#if NETSTANDARD2_0
                await Task.Run(() => Socket.Disconnect(false));
#else
                await Socket.DisconnectAsync(false).ConfigureAwait(false);
#endif
                Logger.LogInformation("{Name} on {LocalEndPoint} disconnected from {RemoteEndPoint} and disposed.", Name, localEndPoint, remoteEndPoint);
            }
            else
            {
                Logger.LogInformation("{Name} on {LocalEndPoint} disposed.", Name, localEndPoint);
            }

            // SocketAcceptor or AsyncEmptyDisposable
            await Disposable.DisposeAsync().ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Logger.LogWarning(e, "DisposeAsync.");
        }
        finally
        {
            Tcs.SetResult(true);
            Socket.Dispose();
            ReceiveCts.Dispose();
        }
    }
}
