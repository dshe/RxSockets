using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
namespace RxSockets;

internal sealed class SocketAcceptor : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly Socket _socket;
    private readonly CancellationToken _serverCt;
    private readonly List<IRxSocketClient> _connections = []; // state

    internal SocketAcceptor(Socket socket, ILogger logger, CancellationToken ct)
    {
        _socket = socket;
        _logger = logger;
        _serverCt = ct;
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
                    Socket acceptSocket = await _socket.AcceptAsync(ct).ConfigureAwait(false);
                    observer.OnNext(CreateClient(acceptSocket));
                }
                catch (Exception e)
                {
                    if (_serverCt.IsCancellationRequested)
                    { 
                        observer.OnCompleted(); 
                        return; 
                    }
                    if (ct.IsCancellationRequested)
                        return;
                    _logger.LogError("SocketAcceptor: {Message}", e.Message);
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
                acceptSocket = await _socket.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (ct.IsCancellationRequested)
                    yield break;
                _logger.LogError("SocketAcceptor: {Message}", e.Message);
                throw;
            }
            yield return CreateClient(acceptSocket);
        }
    }

    private RxSocketClient CreateClient(Socket acceptSocket)
    {
        _logger.LogDebug("AcceptClient on {LocalEndPoint} connected to {RemoteEndPoint}.", _socket.LocalEndPoint, acceptSocket.RemoteEndPoint);
        RxSocketClient client = new(acceptSocket, _logger, "AcceptClient");
        _connections.Add(client);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        IEnumerable<Task> tasks = _connections.Select(client => client.DisposeAsync().AsTask());
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
