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
        ///  Create a connected RxSocketClient.
        /// </summary>
        public static async Task<IRxSocketClient> CreateRxSocketClientAsync(
            this IPEndPoint endPoint, CancellationToken ct = default) =>
                await CreateRxSocketClientAsync(endPoint, NullLogger.Instance, ct)
                    .ConfigureAwait(false);

        /// <summary>
        ///  Create a connected RxSocketClient.
        /// </summary>
        public static async Task<IRxSocketClient> CreateRxSocketClientAsync(
            this IPEndPoint endPoint, ILogger logger, CancellationToken ct = default)
        {
            Socket socket = await ConnectAsync(endPoint, logger, ct)
                .ConfigureAwait(false);
            return new RxSocketClient(socket, logger, "Client");
        }

        private static async Task<Socket> ConnectAsync(IPEndPoint endPoint, ILogger logger, CancellationToken ct)
        {
            Socket socket = Utilities.CreateSocket();
            try
            {
                await socket.ConnectAsync(endPoint, ct).ConfigureAwait(false);
                logger.LogTrace($"Client on {socket.LocalEndPoint} connected to {endPoint}.");
                return socket;
            }
            catch (Exception e)
            {
                string msg = $"Socket could not connect to {endPoint}. {e.Message}";
                if (e is SocketException se)
                {
                    string errorName = "SocketException: " + Enum.GetName(typeof(SocketError), se.ErrorCode);
                    logger.LogTrace(e, $"{msg} {errorName}.");
                }
                else
                    logger.LogTrace(e, msg);
                throw;
            }
        }
    }
}
