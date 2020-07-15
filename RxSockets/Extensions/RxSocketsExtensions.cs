using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace RxSockets
{
    public static class RxSocketsExtensions
    {
        public static Task<IRxSocketClient> ConnectRxSocketClientAsync(this IPEndPoint endPoint,
            CancellationToken ct = default) =>
                ConnectRxSocketClientAsync(endPoint, NullLogger.Instance, ct);

        public static async Task<IRxSocketClient> ConnectRxSocketClientAsync(this IPEndPoint endPoint, 
            ILogger logger, CancellationToken ct = default)
        {
            var socket = await SocketConnector.ConnectAsync(endPoint, logger, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, logger, false);
        }
    }
}
