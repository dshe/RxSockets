using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RxSockets
{
    public static class RxSocketsExtensions
    {
        /// <summary>
        /// Create an RxSocketServer on IPEndPoint.
        /// </summary>
        public static IRxSocketServer CreateRxSocketServer(this IPEndPoint ipEndPoint, int backLog = 10) =>
            RxSocketServer.Create(ipEndPoint, NullLogger.Instance, backLog);

        /// <summary>
        /// Create an RxSocketServer on IPEndPoint.
        /// </summary>
        public static IRxSocketServer CreateRxSocketServer(this IPEndPoint ipEndPoint, ILogger logger, int backLog = 10) =>
            RxSocketServer.Create(ipEndPoint, logger, backLog);

        /// <summary>
        ///  Create a connected RxSocketClient.
        /// </summary>
        public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this IPEndPoint endPoint, CancellationToken ct = default) =>
                await CreateRxSocketClientAsync(endPoint, NullLogger.Instance, ct).ConfigureAwait(false);

        /// <summary>
        ///  Create a connected RxSocketClient.
        /// </summary>
        public static async Task<IRxSocketClient> CreateRxSocketClientAsync(this IPEndPoint endPoint, ILogger logger, CancellationToken ct = default)
        {
            Socket socket = await ConnectAsync(endPoint, logger, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, logger, "Client");
        }

        private static async Task<Socket> ConnectAsync(IPEndPoint endPoint, ILogger logger, CancellationToken ct)
        {
            Socket socket = Utilities.CreateSocket();
            try
            {
                await socket.ConnectAsync(endPoint, ct).ConfigureAwait(false);
                logger.LogTrace("Client on {LocalEndPoint} connected to {EndPoint}.", socket.LocalEndPoint, endPoint);
                return socket;
            }
            catch (Exception e)
            {
                if (e is SocketException se)
                {
                    string errorName = "SocketException: " + Enum.GetName(typeof(SocketError), se.ErrorCode);
                    logger.LogTrace(e, "Socket could not connect to { EndPoint}. {Message} {ErrorName}.", endPoint, e.Message, errorName);
                }
                else
                    logger.LogTrace(e, "Socket could not connect to {EndPoint}. {Message}", endPoint, e.Message);
                throw;
            }
        }
    }
}
