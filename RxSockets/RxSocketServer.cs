using Microsoft.Extensions.Logging.Abstractions;

namespace RxSockets;

public interface IRxSocketServer : IAsyncDisposable
{
    EndPoint LocalEndPoint { get; }
    IAsyncEnumerable<IRxSocketClient> AcceptAllAsync { get; }
}

public sealed class RxSocketServer : IRxSocketServer
{
    private readonly CancellationTokenSource Cts = new();
    private readonly SocketAcceptor Acceptor;
    private readonly SocketDisposer Disposer;
    public EndPoint LocalEndPoint { get; }
    public IAsyncEnumerable<IRxSocketClient> AcceptAllAsync { get; }

    private RxSocketServer(Socket socket, ILogger logger)
    {
        LocalEndPoint = socket.LocalEndPoint ?? throw new InvalidOperationException();
        Acceptor = new SocketAcceptor(socket, logger);
        Disposer = new SocketDisposer(socket, Acceptor, "Server", Cts, logger);
        AcceptAllAsync = Acceptor.AcceptAllAsync(Cts.Token);
        logger.LogInformation("Server on {LocalEndPoint} created.", LocalEndPoint);
    }

    public async ValueTask DisposeAsync()
    {
        await Acceptor.DisposeAsync().ConfigureAwait(false);
        await Disposer.DisposeAsync().ConfigureAwait(false);
        Cts.Dispose();
    }

    /// <summary>
    /// Create a RxSocketServer.
    /// </summary>
    public static IRxSocketServer Create(Socket socket, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(socket);
        return new RxSocketServer(socket, logger);
    }

    /// <summary>
    /// Create an RxSocketServer on EndPoint.
    /// </summary>
    public static IRxSocketServer Create(EndPoint endPoint, ILogger logger, int backLog = 10)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        // Backlog specifies the number of pending connections allowed before a busy error is returned.
        if (backLog < 0)
            throw new ArgumentException($"Invalid backLog: {backLog}.");
        Socket socket = Utilities.CreateSocket();
        socket.Bind(endPoint);
        socket.Listen(backLog);
        return Create(socket, logger);
    }

    /// <summary>
    /// Create a RxSocketServer on an available port on the localhost.
    /// </summary>
    public static IRxSocketServer Create(int backLog = 10) =>
        Create(Utilities.CreateIPEndPointOnPort(0), NullLogger.Instance, backLog);

    /// <summary>
    /// Create a RxSocketServer on an available port on the localhost.
    /// </summary>
    public static IRxSocketServer Create(ILogger logger, int backLog = 10) =>
        Create(Utilities.CreateIPEndPointOnPort(0), logger, backLog);
}
