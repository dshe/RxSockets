using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class RxSocketsEx
    {
        public static async Task<IRxSocketClient> ConnectRxSocketClientAsync(this IPEndPoint endPoint, 
            ILogger? logger = null, int timeout = -1, CancellationToken ct = default)
        {
            logger ??= NullLogger.Instance;
            var socket = await SocketConnector.ConnectAsync(endPoint, logger, timeout, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, logger, false);
        }

        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        public static IRxSocketServer CreateRxSocketServer(this IPEndPoint endPoint,
            ILogger? logger = null, int backLog = 10)
        {
            logger ??= NullLogger.Instance;
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(endPoint);
            socket.Listen(backLog);
            return new RxSocketServer(socket, logger);
        }
    }
}
