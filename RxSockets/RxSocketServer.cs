using System;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;
using System.Threading;

namespace RxSockets
{
    public interface IRxSocketServer: IAsyncDisposable
    {
        IPEndPoint IPEndPoint { get; }
        IObservable<IRxSocketClient> AcceptObservable { get; }
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        private readonly CancellationTokenSource Cts = new();
        private readonly SocketAcceptor Acceptor;
        private readonly SocketDisposer Disposer;
        public IPEndPoint IPEndPoint { get; }
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        private RxSocketServer(Socket socket, ILogger logger)
        {
            IPEndPoint = (IPEndPoint)(socket.LocalEndPoint ?? throw new ArgumentException("LocalEndPoint"));
            Acceptor = new SocketAcceptor(socket, logger);
            AcceptObservable = Acceptor.CreateAcceptObservable(Cts.Token);
            Disposer = new SocketDisposer(socket, Cts, logger, "Server", Acceptor);
            logger.LogTrace($"Server created on {IPEndPoint}.");
        }

        public async ValueTask DisposeAsync() =>
            await Disposer.DisposeAsync().ConfigureAwait(false);

        /// <summary>
        /// Create an RxSocketServer on a random port on the localhost.
        /// </summary>
        public static IRxSocketServer Create(int backLog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Loopback, 0), NullLogger.Instance, backLog);

        /// <summary>
        /// Create an RxSocketServer on a random port on the localhost.
        /// </summary>
        public static IRxSocketServer Create(ILogger logger, int backLog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Loopback, 0), logger, backLog);

        /// <summary>
        /// Create an RxSocketServer on IPEndPoint.
        /// </summary>
        public static IRxSocketServer Create(IPEndPoint ipEndPoint, int backLog = 10) =>
            Create(ipEndPoint, NullLogger.Instance, backLog);

        /// <summary>
        /// Create an RxSocketServer on IPEndPoint.
        /// </summary>
        public static IRxSocketServer Create(IPEndPoint ipEndPoint, ILogger logger, int backLog = 10)
        {
            // Backlog specifies the number of pending connections allowed before a busy error is returned.
            if (backLog < 0)
                throw new ArgumentException($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(ipEndPoint);
            socket.Listen(backLog);
            return new RxSocketServer(socket, logger);
        }
    }
}
