using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
namespace RxSockets;

[SuppressMessage("Usage", "CA1031:Catch more specific exception type.")]
internal sealed class SocketReceiver
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly string Name;
    private readonly byte[] Buffer = new byte[0x10000];
    private int Position;
    private int BytesReceived;

    internal SocketReceiver(Socket socket, string name, ILogger logger)
    {
        Socket = socket;
        Name = name;
        Logger = logger;
    }

    internal IObservable<byte> CreateReceiveObservable()
    {
        Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");
        ;

        return Observable.Create<byte>(async (observer, ct) =>
        {
            Logger.LogDebug("{Name}: SocketReceiverObservable subscribing.", Name);

            Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");
            ;
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
                    Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");

                    observer.OnNext(Buffer[Position++]);
                }
            }
            catch (Exception ex)
            {
                if (ct.IsCancellationRequested)
                    return;
                Logger.LogDebug(ex, "{Name} on {LocalEndPoint} SocketReceiverObservable Exception: {Message}", Name, Socket.LocalEndPoint, ex.Message);
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
                    if (ct.IsCancellationRequested)
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
