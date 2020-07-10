using System;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using System.Threading;
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
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly SocketDisposer Disposer;
        private readonly SocketAccepter SocketAccepter;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        public RxSocketServer(int backLog = 10, ILogger? logger = null) :
            this(new IPEndPoint(IPAddress.IPv6Loopback, 0), backLog, logger) {}

        public RxSocketServer(IPEndPoint ipEndPoint, int backLog = 10, ILogger? logger = null)
        {
            // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
            Logger  = logger ?? NullLogger.Instance;
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            var socket = Utilities.CreateSocket();
            ipEndPoint ??= new IPEndPoint(IPAddress.IPv6Loopback, 0); // autoselect
            socket.Bind(ipEndPoint);
            socket.Listen(backLog);
            IPEndPoint = (IPEndPoint)socket.LocalEndPoint;
            Logger.LogDebug($"RxSocketServer created on {IPEndPoint}.");
            Disposer = new SocketDisposer(socket, "RxSocketServer", Logger);
            SocketAccepter = new SocketAccepter(socket, Cts.Token, Logger);
            AcceptObservable = SocketAccepter.CreateAcceptObservable();
        }

        public async Task DisposeAsync()
        {
            Cts.Cancel();
            var tasks = SocketAccepter.Clients.Select(client => client.DisposeAsync()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            await Disposer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
