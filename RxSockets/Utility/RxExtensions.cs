using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets.Utility
{
    public static class RxExtensions
    {
        public static IRxSocketServer Create(this IPEndPoint endPoint, int backLog = 10) =>
            Create(endPoint, NullLogger<RxSocketServer>.Instance, backLog);

        public static IRxSocketServer Create(this IPEndPoint endPoint, ILogger<RxSocketServer> logger, int backLog = 10)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            logger.LogInformation($"Creating server at EndPoint: {endPoint}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(endPoint);
            socket.Listen(backLog);
            logger.LogTrace("Listening.");
            return new RxSocketServer(socket, logger);
        }
        public static Task<IRxSocketClient> ConnectAsync(this IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
            => ConnectAsync(endPoint, NullLogger<RxSocketClient>.Instance, timeout, ct);

        public static async Task<IRxSocketClient> ConnectAsync(this IPEndPoint endPoint, ILogger<RxSocketClient> logger, int timeout = -1, CancellationToken ct = default)
        {
            var socket = await SocketConnector.ConnectAsync(endPoint, logger, timeout, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, logger);
        }
    }
}
