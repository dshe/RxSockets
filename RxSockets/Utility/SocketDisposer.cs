using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace RxSockets;

internal sealed class SocketDisposer : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly IAsyncDisposable? Disposable;
    private readonly string Name;
    private readonly TaskCompletionSource<bool> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource ReceiveCts;
    private int Disposals; // state
    internal bool DisposeRequested => Disposals > 0;

    internal SocketDisposer(Socket socket, CancellationTokenSource receiveCts, ILogger logger, string name, IAsyncDisposable? disposable = null)
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
                await Socket.DisconnectAsync(false).ConfigureAwait(false);
                Logger.LogDebug("{Name} on {LocalEndPoint} disconnected from {RemoteEndPoint} and disposed.", Name, localEndPoint, remoteEndPoint);
            }
            else
            {
                Logger.LogDebug("{Name} on {LocalEndPoint} disposed.", Name, localEndPoint);
            }

            if (Disposable is not null) // SocketAcceptor
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
