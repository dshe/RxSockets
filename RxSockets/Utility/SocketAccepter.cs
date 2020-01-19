using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    class SocketAccepter
    {
        private readonly CancellationToken Ct;
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();

        internal SocketAccepter(Socket socket, CancellationToken ct, ILogger logger)
        {
            Socket = socket;
            Ct = ct;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
        }

        internal IObservable<Socket> CreateAcceptObservable()
        {
            return Observable.Create<Socket>(async observer =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
                var ct = cts.Token;
                try
                {
                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();
                        var socket = await AcceptAsync().ConfigureAwait(false);
                        observer.OnNext(socket);
                    }
                }
                catch (Exception e)
                {
                    if (cts.IsCancellationRequested && !Ct.IsCancellationRequested)
                        observer.OnCompleted(); // unsubscribe
                    else
                    {
                        try
                        {
                            if (!ct.IsCancellationRequested)
                                Logger.LogDebug($"SocketAcceptor on {Socket.LocalEndPoint} Observer Exeption\r\n{e}");
                            observer.OnError(e);
                        }
                        catch (Exception e2)
                        {
                            if (!ct.IsCancellationRequested)
                                Logger.LogDebug($"SocketAcceptor on {Socket.LocalEndPoint} OnError Exeption\r\n{e2}");
                        }
                    }
                }
                return Disposable.Create(() => cts.Cancel());
            });
        }

        private async Task<Socket> AcceptAsync()
        {
            Ct.ThrowIfCancellationRequested();
            Args.AcceptSocket = Utilities.CreateSocket();
            if (Socket.AcceptAsync(Args))
                await Semaphore.WaitAsync(Ct).ConfigureAwait(false);
            return Args.AcceptSocket;
        }
    }
}
