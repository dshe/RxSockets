using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
using System.Collections.Generic;
using System.Threading;
using RxSockets.Utility;
#nullable enable

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
        private CancellationTokenSource? Cts = null;
        private readonly SocketDisposer Disposer;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        internal RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            Disposer = new SocketDisposer(socket, logger);
            AcceptObservable = CreateAcceptObservable(socket);
            Logger.LogTrace("RxSocketServer Constructed.");
        }

        private IObservable<IRxSocketClient> CreateAcceptObservable(Socket socket)
        {
            var semaphore = new SemaphoreSlim(0, 1);
            void handler(object sender, SocketAsyncEventArgs a) => semaphore.Release();
            var args = new SocketAsyncEventArgs();
            args.Completed += handler;

            return Observable.Create<IRxSocketClient>(observer =>
            {
                Cts = new CancellationTokenSource();

                NewThreadScheduler.Default.Schedule(() =>
                {
                    Logger.LogTrace("Starting Accept.");

                    try
                    {
                        while (!Cts!.IsCancellationRequested)
                        {
                            args.AcceptSocket = Utilities.CreateSocket();
                            if (socket.AcceptAsync(args))
                                semaphore.Wait(Cts.Token);
                            Logger.LogInformation($"Accepted client: {args.AcceptSocket!.LocalEndPoint}.");
                            var acceptClient = new RxSocketClient(args.AcceptSocket, Logger);
                            Clients.Add(acceptClient);
                            observer.OnNext(acceptClient);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogTrace("Accept Ended."); // crashes logger
                        if (!Cts!.IsCancellationRequested && !Disposer.DisposeRequested)
                            Logger.LogInformation(e, "Async Exception.");
                        observer.OnCompleted();
                    }
                });

                return Disposable.Create(() => Cts?.Cancel());
            });
        }
        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10) => RxExtensions.CreateRxSocketServer(endPoint, backLog);
        public static IRxSocketServer Create(IPEndPoint endPoint, ILogger<RxSocketServer> logger, int backLog = 10) => RxExtensions.CreateRxSocketServer(endPoint, logger, backLog);
        public void Dispose()
        {
            Cts?.Cancel();
            foreach (var client in Clients)
                client.Dispose();
            Disposer.Dispose();
        }
    }
}
