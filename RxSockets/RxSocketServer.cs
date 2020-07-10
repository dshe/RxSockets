using System;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    public interface IRxSocketServer
    {
        IObservable<IRxSocketClient> AcceptObservable { get; }
        Task DisposeAsync();
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        private readonly ILogger Logger;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly SocketDisposer Disposer;
        private readonly SocketAccepter SocketAccepter;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        internal RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            Logger.LogDebug($"RxSocketServer created on {socket.LocalEndPoint}.");
            Disposer = new SocketDisposer(socket, "RxSocketServer", logger);
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
