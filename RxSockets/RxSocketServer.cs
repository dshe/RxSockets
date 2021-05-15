using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Sockets;

namespace RxSockets
{
    public interface IRxSocketServer
    {
        IPEndPoint IPEndPoint { get; }
        IObservable<IRxSocketClient> AcceptObservable { get; }
        Task DisposeAsync();
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        private readonly ILogger Logger;
        public IPEndPoint IPEndPoint { get; }
        private readonly SocketDisposer Disposer;
        private readonly SocketAccepter SocketAccepter;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        public static IRxSocketServer Create(int backLog = 10) =>
            CreateOnEndPoint(new IPEndPoint(IPAddress.IPv6Loopback, 0), NullLogger.Instance, backLog);

        public static IRxSocketServer Create(ILogger logger, int backLog = 10) =>
            CreateOnEndPoint(new IPEndPoint(IPAddress.IPv6Loopback, 0), logger, backLog);

        public static IRxSocketServer CreateOnEndPoint(IPEndPoint ipEndPoint, int backLog = 10) =>
            CreateOnEndPoint(ipEndPoint, NullLogger.Instance, backLog);

        public static IRxSocketServer CreateOnEndPoint(IPEndPoint ipEndPoint, ILogger logger, int backLog = 10)
        {
            // Backlog specifies the number of pending connections allowed before a busy error is returned.
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(ipEndPoint);
            socket.Listen(backLog);
            return new RxSocketServer(socket, logger);
        }

        private RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            IPEndPoint = (IPEndPoint)(socket.LocalEndPoint ?? throw new Exception("LocalEndPoint"));
            Disposer = new SocketDisposer(socket, "RxSocketServer", Logger);
            SocketAccepter = new SocketAccepter(socket, Logger);
            AcceptObservable = SocketAccepter.CreateAcceptObservable();
            Logger.LogDebug($"RxSocketServer created on {IPEndPoint}.");
        }

        public async Task DisposeAsync()
        {
            var tasks = SocketAccepter.AcceptedClients.Select(client => client.DisposeAsync()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await Disposer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
