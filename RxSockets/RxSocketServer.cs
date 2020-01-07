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
            Disposer = new SocketDisposer(socket, "RxSocketServer", logger);
            SocketAccepter = new SocketAccepter(socket, logger, Cts.Token);
            AcceptObservable = SocketAccepter.Accept()
                .ToObservable(NewThreadScheduler.Default.Catch<Exception>(ExceptionHandler))
                .Select(acceptSocket => new RxSocketClient(acceptSocket, true, logger))
                .Do(client => Clients.Add(client));
            Logger.LogDebug($"RxSocketServer created on {socket.LocalEndPoint}.");
        }

        private bool ExceptionHandler(Exception e)
        {
            if (!Cts.IsCancellationRequested)
                Logger.LogWarning($"RxSocketServer scheduler caught {e.ToString()}.");
            return true;
        }

        public async Task DisposeAsync()
        {
            Cts.Cancel();
            var disposeTasks = Clients.Select(client => client.DisposeAsync()).ToList();
            disposeTasks.Add(Disposer.DisposeAsync());
            await Task.WhenAll(disposeTasks).ConfigureAwait(false);
        }
    }
}
