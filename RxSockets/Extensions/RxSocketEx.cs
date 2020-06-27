using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class RxSocketEx
    {
        public static async Task<IRxSocketClient> ConnectRxSocketClientAsync(this IPEndPoint endPoint, int timeout = -1, ILogger<RxSocketClient>? logger = null, CancellationToken ct = default)
        {
            logger ??= NullLogger<RxSocketClient>.Instance;
            var socket = await SocketConnector.ConnectAsync(endPoint, logger, timeout, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, false, logger);
        }

        public static IRxSocketServer CreateRxSocketServer(this IPEndPoint endPoint, int backLog = 10, ILogger<RxSocketServer>? logger = null)
        {
            logger ??= NullLogger<RxSocketServer>.Instance;
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(endPoint);
            socket.Listen(backLog);
            return new RxSocketServer(socket, logger);
        }
    }
}
