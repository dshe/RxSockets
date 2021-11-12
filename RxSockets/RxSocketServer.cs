using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

namespace RxSockets
{
    public interface IRxSocketServer: IAsyncDisposable
    {
        IPEndPoint IPEndPoint { get; }
        IAsyncEnumerable<IRxSocketClient> AcceptAllAsync();
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        private readonly CancellationTokenSource Cts = new();
        private readonly SocketAcceptor Acceptor;
        private readonly SocketDisposer Disposer;
        public IPEndPoint IPEndPoint { get; }

        private RxSocketServer(Socket socket, ILogger logger)
        {
            IPEndPoint = (IPEndPoint)(socket.LocalEndPoint ?? throw new ArgumentException("LocalEndPoint"));
            Acceptor = new SocketAcceptor(socket, logger);
            Disposer = new SocketDisposer(socket, Cts, logger, "Server", Acceptor);
            logger.LogTrace("Server created on {IPEndPoint}.", IPEndPoint);
        }

        public IAsyncEnumerable<IRxSocketClient> AcceptAllAsync() => Acceptor.AcceptAllAsync(Cts.Token);

        public async ValueTask DisposeAsync() =>
            await Disposer.DisposeAsync().ConfigureAwait(false);

        /// <summary>
        /// Create an RxSocketServer.
        /// </summary>
        public static IRxSocketServer Create(Socket socket, ILogger logger) => new RxSocketServer(socket, logger);

        /// <summary>
        /// Create an RxSocketServer on IPEndPoint.
        /// </summary>
        public static IRxSocketServer Create(IPEndPoint ipEndPoint, ILogger logger, int backLog = 10)
        {
            // Backlog specifies the number of pending connections allowed before a busy error is returned.
            if (backLog < 0)
                throw new ArgumentException($"Invalid backLog: {backLog}.");
            Socket socket = Utilities.CreateSocket();
            socket.Bind(ipEndPoint);
            socket.Listen(backLog);
            return Create(socket, logger);
        }

        /// <summary>
        /// Create an RxSocketServer on an available port on the localhost.
        /// </summary>
        public static IRxSocketServer Create(int backLog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Loopback, 0), NullLogger.Instance, backLog);

        /// <summary>
        /// Create an RxSocketServer on an available port on the localhost.
        /// </summary>
        public static IRxSocketServer Create(ILogger logger, int backLog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Loopback, 0), logger, backLog);
    }
}
