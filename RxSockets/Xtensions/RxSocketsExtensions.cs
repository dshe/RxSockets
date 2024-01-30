using Microsoft.Extensions.Logging.Abstractions;

namespace RxSockets;

public static partial class Xtensions
{
    /// <summary>
    /// Create a connected RxSocketClient.
    /// </summary>
    public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this EndPoint endPoint, CancellationToken ct = default) =>
            await CreateRxSocketClientAsync(endPoint, NullLogger.Instance, ct).ConfigureAwait(false);

    /// <summary>
    /// Create a connected RxSocketClient.
    /// </summary>
    public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this EndPoint endPoint, ILoggerFactory factoryLogger, CancellationToken ct = default) =>
            await CreateRxSocketClientAsync(endPoint, factoryLogger.CreateLogger<RxSocketClient>(), ct).ConfigureAwait(false);

    /// <summary>
    /// Create a connected RxSocketClient.
    /// </summary>
    public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this EndPoint endPoint, ILogger logger, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        Socket socket = await ConnectAsync(endPoint, logger, ct).ConfigureAwait(false);
        return new RxSocketClient(socket, logger, "Client");
    }

    private static async Task<Socket> ConnectAsync(EndPoint endPoint, ILogger logger, CancellationToken ct)
    {
        Socket socket = Utilities.CreateSocket();
        try
        {
            await socket.ConnectAsync(endPoint, ct).ConfigureAwait(false);
            logger.LogInformation("Client on {LocalEndPoint} connected to {EndPoint}.", socket.LocalEndPoint, endPoint);
            return socket;
        }
        catch (Exception e)
        {
            if (e is SocketException se)
            {
                string errorName = $"SocketException: {Enum.GetName(typeof(SocketError), se.ErrorCode)}";
                logger.LogWarning("Socket could not connect to {EndPoint}. {Message} {ErrorName}.", endPoint, e.Message, errorName);
            }
            else
                logger.LogWarning("Socket could not connect to {EndPoint}. {Message}", endPoint, e.Message);
            throw;
        }
    }

    /// <summary>
    /// Create an RxSocketServer on EndPoint.
    /// </summary>
    public static IRxSocketServer CreateRxSocketServer(this EndPoint endPoint, int backLog = 10) =>
        RxSocketServer.Create(endPoint, NullLoggerFactory.Instance, backLog);

    /// <summary>
    /// Create an RxSocketServer on EndPoint.
    /// </summary>
    public static IRxSocketServer CreateRxSocketServer(this EndPoint endPoint, ILoggerFactory loggerFactory, int backLog = 10) =>
        RxSocketServer.Create(endPoint, loggerFactory, backLog);
}
