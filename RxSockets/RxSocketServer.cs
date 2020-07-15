using System;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

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

        // IPv6Loopback on auto-selected port.
        public RxSocketServer(ILogger? logger = null, int backLog = 10) :
            this(new IPEndPoint(IPAddress.IPv6Loopback, 0), logger, backLog) { }

        public RxSocketServer(IPEndPoint ipEndPoint, ILogger? logger = null, int backLog = 10)
        {
            // Backlog specifies the number of pending connections allowed before a busy error is returned.
            Logger = logger ?? NullLogger.Instance;
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(ipEndPoint);
            socket.Listen(backLog);
            IPEndPoint = (IPEndPoint)socket.LocalEndPoint;
            Logger.LogDebug($"RxSocketServer created on {IPEndPoint}.");
            Disposer = new SocketDisposer(socket, "RxSocketServer", Logger);
            SocketAccepter = new SocketAccepter(socket, Logger);
            AcceptObservable = SocketAccepter.CreateAcceptObservable();
        }

        public async Task DisposeAsync()
        {
            var tasks = SocketAccepter.AcceptedClients.Select(client => client.DisposeAsync()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await Disposer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
