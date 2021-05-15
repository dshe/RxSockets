using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    class SocketAccepter
    {
        private readonly ILogger Logger;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1); // start with wait
        private readonly Socket Socket;
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();
        internal readonly List<RxSocketClient> AcceptedClients = new List<RxSocketClient>();

        internal SocketAccepter(Socket socket, ILogger logger)
        {
            Socket = socket;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
        }

        internal IObservable<IRxSocketClient> CreateAcceptObservable()
        {
            return Observable.Create<IRxSocketClient>((observer, ct) =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            ct.ThrowIfCancellationRequested();
                            Args.AcceptSocket = Utilities.CreateSocket();
                            if (Socket.AcceptAsync(Args))
                                Semaphore.Wait(ct);
                            var client = new RxSocketClient(Args.AcceptSocket, Logger, isAcceptSocket: true);
                            AcceptedClients.Add(client);
                            observer.OnNext(client);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogDebug(e, $"SocketAcceptor on {Socket.LocalEndPoint}. {e.Message}");
                        observer.OnCompleted();
                    }
                }, ct);
            });
        }
    }
}
