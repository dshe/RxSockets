using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
using System.Collections.Generic;

#nullable enable

namespace RxSockets
{
    public interface IRxSocketServer: IDisposable
    {
        IObservable<IRxSocketClient> AcceptObservable { get; }
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly ILogger Logger;
        private readonly List<RxSocketClient> Clients = new List<RxSocketClient>();
        private readonly SocketDisposer Disposer;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        private RxSocketServer(Socket socket, ILogger logger)
        {
            Logger = logger;
            Disposer = new SocketDisposer(socket, logger);
            AcceptObservable = CreateAcceptObservable(socket);
        }

        private IObservable<IRxSocketClient> CreateAcceptObservable(Socket socket)
        {
            // supports a single observer
            return Observable.Create<IRxSocketClient>(observer =>
            {
                NewThreadScheduler.Default.Schedule(() =>
                {
                    Logger.LogInformation($"Listening.");
                    try
                    {
                        while (true)
                        {
                            var accept = socket.Accept();
                            Logger.LogInformation($"Accepted client: {accept.LocalEndPoint}.");
                            var acceptClient = new RxSocketClient(accept, Logger);
                            Clients.Add(acceptClient);
                            observer.OnNext(acceptClient);
                        }
                    }
                    catch (Exception e)
                    {
                        //Logger.LogTrace("Accept Ended."); // crashes logger
                        if (!Disposer.DisposeRequested)
                            Logger.LogInformation(e, "AcceptAsync Exception.");
                        observer.OnCompleted();
                    }
                });

                return Disposable.Empty;
            });
        }

        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10) =>
            Create(endPoint, NullLogger<RxSocketServer>.Instance, backLog);

        public static IRxSocketServer Create(IPEndPoint endPoint, ILogger<RxSocketServer> logger, int backLog = 10)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));
            if (backLog < 0)
                throw new Exception($"Invalid backLog: {backLog}.");
            logger.LogInformation($"Creating server at EndPoint: {endPoint}.");
            var socket = Utilities.CreateSocket();
            socket.Bind(endPoint);
            socket.Listen(backLog);
            return new RxSocketServer(socket, logger);
        }

        public void Dispose()
        {
            foreach (var client in Clients)
                client.Dispose();
            Disposer.Dispose();
        }
    }
}
