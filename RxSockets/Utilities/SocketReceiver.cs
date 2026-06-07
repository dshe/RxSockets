using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
namespace RxSockets;

internal sealed class SocketReceiver
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly CancellationToken ReceiveCt;
    private readonly string Name;
    private readonly byte[] Buffer = new byte[0x10000];
    private int Position, BytesReceived;

    internal SocketReceiver(Socket socket, string name, ILogger logger, CancellationToken receiveCt)
    {
        Logger = logger;
        Socket = socket;
        ReceiveCt = receiveCt;
        Name = name;
    }

    internal IObservable<byte> CreateReceiveObservable0()
    {
        return Observable.Create<byte>(async (observer, ct) =>
        {
            Logger.LogDebug("{Name}: SocketReceiverObservable subscribing.", Name);
            Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (Position == BytesReceived)
                    {
                        BytesReceived = await Socket.ReceiveAsync(Buffer, ct).ConfigureAwait(false);
                        Position = 0;

                        if (BytesReceived == 0)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        Logger.LogReceive(Name, Socket.LocalEndPoint, BytesReceived, Socket.RemoteEndPoint);
                    }
                    observer.OnNext(Buffer[Position++]);
                }
            }
            catch (Exception ex)
            {
                if (ReceiveCt.IsCancellationRequested)
                {
                    observer.OnCompleted();
                    return;
                }
                if (ct.IsCancellationRequested)
                    return;
                Logger.LogDebug(ex, "{Name}: SocketReceiverObservable Exception: {Message}", Name, ex.Message);
                observer.OnError(ex);
            }
        });
    }

    internal IObservable<byte> CreateReceiveObservable()
    {
        return Observable.Create<byte>(async (observer, ct) =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (Position >= BytesReceived)
                    {
                        BytesReceived = await Socket.ReceiveAsync(Buffer, ct).ConfigureAwait(false);
                        Position = 0;
                        if (BytesReceived <= 0)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        Logger.LogReceive(Name, Socket.LocalEndPoint, BytesReceived, Socket.RemoteEndPoint);
                    }

                    observer.OnNext(Buffer[Position]);
                    Position++;
                }
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted();
            }
            catch (ObjectDisposedException)
            {
                observer.OnCompleted();
            }
            catch (SocketException) when (ct.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                Logger.LogDebug(
                    ex,
                    "{Name}: SocketReceiverObservable Exception: {Message}",
                    Name,
                    ex.Message);

                observer.OnError(ex);
            }
        });
    }

    internal async IAsyncEnumerable<byte> ReceiveAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (Position == BytesReceived)
            {
                try
                {
                    BytesReceived = await Socket.ReceiveAsync(Buffer, ct).ConfigureAwait(false);
                    Position = 0;
                }
                catch (Exception)
                {
                    if (ct.IsCancellationRequested || ReceiveCt.IsCancellationRequested)
                        yield break;
                    throw;
                }

                if (BytesReceived == 0)
                    yield break;

                Logger.LogReceive(Name, Socket.LocalEndPoint, BytesReceived, Socket.RemoteEndPoint);
            }
            yield return Buffer[Position++];
        }
    }
}
