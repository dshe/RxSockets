using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    }
}
