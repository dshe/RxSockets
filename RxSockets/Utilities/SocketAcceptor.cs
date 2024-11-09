using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
namespace RxSockets;

[SuppressMessage("Usage", "CA1031:Catch more specific exception type.")]
internal sealed class SocketAcceptor : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly List<IRxSocketClient> Connections = []; // state

    internal SocketAcceptor(Socket socket, ILogger logger)
    {
        Socket = socket;
        Logger = logger;
    }

    internal IObservable<IRxSocketClient> CreateAcceptObservable()
    {
        return Observable.Create<IRxSocketClient>(async (observer, ct) =>
        {
            Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Socket acceptSocket = await Socket.AcceptAsync(ct).ConfigureAwait(false);
                    observer.OnNext(CreateClient(acceptSocket));
                }
                catch (Exception e)
                {
                    if (ct.IsCancellationRequested)
                        return;
                    Logger.LogError("SocketAcceptor on {LocalEndPoint}. {Message}", Socket.LocalEndPoint, e.Message);
                    observer.OnError(e);
                    return;
                }
            }
        });
    }

    // CancellationToken is linked to the one in the producer using CreateLinkedTokenSource so that
    // consumer can cancel using cooperative cancellation implemented in the producer so not only
    // we can cancel consuming, but also, producing.\
    // ConfiguredCancelableAsyncEnumerable ae = AcceptAllAsync.WithCancelation(ct);
    internal async IAsyncEnumerable<IRxSocketClient> CreateAcceptAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        Socket acceptSocket;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                acceptSocket = await Socket.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (ct.IsCancellationRequested)
                    yield break;
                Logger.LogError("SocketAcceptor on {LocalEndPoint}. {Message}", Socket.LocalEndPoint, e.Message);
                throw;
            }
            yield return CreateClient(acceptSocket);
        }
    }

    private RxSocketClient CreateClient(Socket acceptSocket)
    {
        Logger.LogDebug("AcceptClient on {LocalEndPoint} connected to {RemoteEndPoint}.", Socket.LocalEndPoint, acceptSocket.RemoteEndPoint);
        RxSocketClient client = new(acceptSocket, Logger, "AcceptClient");
        Connections.Add(client);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        List<Task> tasks = Connections.Select(client => client.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
