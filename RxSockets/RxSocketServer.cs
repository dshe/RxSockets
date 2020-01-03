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

namespace RxSockets
{
    public interface IRxSocketServer : IDisposable
    {
        IObservable<IRxSocketClient> AcceptObservable { get; }
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly ILogger Logger;
        private readonly List<RxSocketClient> Clients = new List<RxSocketClient>();
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly SocketDisposer Disposer;
        private readonly SocketAcceptReader SocketAcceptReader;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        internal RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            Disposer = new SocketDisposer(socket, logger);
            SocketAcceptReader = new SocketAcceptReader(socket, logger, Cts.Token);

            AcceptObservable = SocketAcceptReader.Read()
                .ToObservable(NewThreadScheduler.Default)
                .Select(acceptSocket => new RxSocketClient(acceptSocket, logger));

            Logger.LogTrace("RxSocketServer constructed.");
        }

        public void Dispose()
        {
            Cts.Cancel();
            foreach (var client in Clients)
                client.Dispose();
            Disposer.Dispose();
        }
    }
}
