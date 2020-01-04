using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
using System.Collections.Generic;
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
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly ILogger Logger;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly SocketDisposer Disposer;
        private readonly SocketAccepter SocketAccepter;
        private readonly List<RxSocketClient> Clients = new List<RxSocketClient>();
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        internal RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            Disposer = new SocketDisposer(socket, logger);
            SocketAccepter = new SocketAccepter(socket, logger, Cts.Token);
            AcceptObservable = SocketAccepter.Accept()
                .ToObservable(NewThreadScheduler.Default)
                .Select(acceptSocket => new RxSocketClient(acceptSocket, logger))
                .Do(client => Clients.Add(client));
            Logger.LogTrace($"RxSocketServer constructed on {socket.LocalEndPoint}.");
        }

        public async Task DisposeAsync()
        {
            Cts.Cancel();
            await Disposer.DisposeAsync().ConfigureAwait(false);
            await Task.WhenAll(Clients.Select(client => client.DisposeAsync()));
        }
    }
}
