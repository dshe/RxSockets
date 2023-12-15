using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;

namespace RxSockets;

public static partial class Xtensions
{
    /// <summary>
    ///  Create a connected RxSocketClient.
    /// </summary>
    public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this EndPoint endPoint, CancellationToken ct = default) =>
            await CreateRxSocketClientAsync(endPoint, NullLogger.Instance, ct).ConfigureAwait(false);

    /// <summary>
    ///  Create a connected RxSocketClient.
    /// </summary>
    public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this EndPoint endPoint, ILogger logger, CancellationToken ct = default)
    {
        if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));

        Socket socket = await ConnectAsync(endPoint, logger, ct).ConfigureAwait(false);
        return new RxSocketClient(socket, logger, "Client");
    }

    private static async Task<Socket> ConnectAsync(EndPoint endPoint, ILogger logger, CancellationToken ct)
    {
        Socket socket = Utilities.CreateSocket();
        try
        {
#if NETSTANDARD2_0
            await Task.Run(() => socket.Connect(endPoint), ct).ConfigureAwait(false);
#else
            await socket.ConnectAsync(endPoint, ct).ConfigureAwait(false);
#endif
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
        RxSocketServer.Create(endPoint, NullLogger.Instance, backLog);

    /// <summary>
    /// Create an RxSocketServer on EndPoint.
    /// </summary>
    public static IRxSocketServer CreateRxSocketServer(this EndPoint endPoint, ILogger logger, int backLog = 10) =>
        RxSocketServer.Create(endPoint, logger, backLog);
}
