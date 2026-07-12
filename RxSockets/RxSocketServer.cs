namespace RxSockets;

public interface IRxSocketServer : IAsyncDisposable
{
    EndPoint LocalEndPoint { get; }
    IObservable<IRxSocketClient> AcceptObservable { get; }
    IAsyncEnumerable<IRxSocketClient> AcceptAllAsync { get; }
}

public sealed class RxSocketServer : IRxSocketServer
{
    private readonly CancellationTokenSource _cts = new();
    private readonly SocketAcceptor _acceptor;
    private readonly SocketDisposer _disposer;
    public EndPoint LocalEndPoint { get; }
    public IObservable<IRxSocketClient> AcceptObservable { get; }
    public IAsyncEnumerable<IRxSocketClient> AcceptAllAsync { get; }

    private RxSocketServer(Socket socket, ILogger logger)
    {
        LocalEndPoint = socket.LocalEndPoint ?? throw new InvalidOperationException();
        _acceptor = new SocketAcceptor(socket, logger, _cts.Token);
        _disposer = new SocketDisposer(socket, _acceptor, "Server", logger, _cts);
        AcceptObservable = _acceptor.CreateAcceptObservable();
        AcceptAllAsync = _acceptor.CreateAcceptAllAsync(_cts.Token);
        logger.LogInformation("Server on {LocalEndPoint} created.", LocalEndPoint);
    }

    public async ValueTask DisposeAsync()
    {
        await _acceptor.DisposeAsync().ConfigureAwait(false);
        await _disposer.DisposeAsync().ConfigureAwait(false);
        _cts.Dispose();
    }

    /// <summary>
    /// Create a RxSocketServer.
    /// </summary>
    public static IRxSocketServer Create(Socket socket, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(socket);
        return new RxSocketServer(socket, loggerFactory.CreateLogger<RxSocketServer>());
    }

    /// <summary>
    /// Create an RxSocketServer on EndPoint.
    /// </summary>
    public static IRxSocketServer Create(EndPoint endPoint, ILoggerFactory loggerFactory, int backLog = 10)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        // Backlog specifies the number of pending connections allowed before a busy error is returned.
        if (backLog < 0)
            throw new ArgumentException($"Invalid backLog: {backLog}.");
        Socket socket = Utilities.CreateSocket();
        socket.Bind(endPoint);
        socket.Listen(backLog);
        return Create(socket, loggerFactory);
    }

    /// <summary>
    /// Create a RxSocketServer on an available port on the localhost.
    /// </summary>
    public static IRxSocketServer Create(int backLog = 10) =>
        Create(Utilities.CreateIPEndPointOnPort(0), NullLoggerFactory.Instance, backLog);

    /// <summary>
    /// Create a RxSocketServer on an available port on the localhost.
    /// </summary>
    public static IRxSocketServer Create(ILoggerFactory loggerFactory, int backLog = 10) =>
        Create(Utilities.CreateIPEndPointOnPort(0), loggerFactory, backLog);
}
