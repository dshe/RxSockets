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
        private readonly CancellationToken Ct_disposed;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly Socket Socket;
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();
        internal readonly List<RxSocketClient> Clients = new List<RxSocketClient>();

        internal SocketAccepter(Socket socket, CancellationToken ct_disposed, ILogger logger)
        {
            Socket = socket;
            Ct_disposed = ct_disposed;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
        }

        internal IObservable<IRxSocketClient> CreateAcceptObservable()
        {
            return Observable.Create<IRxSocketClient>((observer, ct_unsubscribed) =>
            {
                return Task.Run(() =>
                {
                    var ct = CancellationTokenSource.CreateLinkedTokenSource(Ct_disposed, ct_unsubscribed).Token;
                    try
                    {
                        while (true)
                        {
                            ct.ThrowIfCancellationRequested();
                            Args.AcceptSocket = Utilities.CreateSocket();
                            if (Socket.AcceptAsync(Args))
                                Semaphore.Wait(ct);
                            var client = new RxSocketClient(Args.AcceptSocket, Logger, true);
                            Clients.Add(client);
                            observer.OnNext(client);
                        }
                    }
                    catch (Exception e)
                    {
                        if (ct_unsubscribed.IsCancellationRequested)
                            return;
                        if (Ct_disposed.IsCancellationRequested)
                            observer.OnError(e);
                        else
                        {
                            Logger.LogDebug($"SocketAcceptor on {Socket.LocalEndPoint} Observer Exception: {e.Message}\r\n{e}");
                            observer.OnError(e);
                        }
                    }
                });
            });
        }
    }
}
