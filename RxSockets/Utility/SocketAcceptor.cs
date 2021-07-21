using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    class SocketAcceptor : IAsyncDisposable
    {
        private readonly ILogger Logger;
        private readonly SemaphoreSlim Semaphore = new(0, 1); // start with wait
        private readonly Socket Socket;
        private readonly SocketAsyncEventArgs Args = new();
        private readonly List<IRxSocketClient> AcceptedClients = new(); // state
        private void ArgsCompleted(object? sender, SocketAsyncEventArgs e) => Semaphore.Release();

        internal SocketAcceptor(Socket socket, ILogger logger)
        {
            Socket = socket;
            Logger = logger;
            Args.Completed += ArgsCompleted;
        }

        internal async IAsyncEnumerable<IRxSocketClient> AcceptAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                Args.AcceptSocket = Utilities.CreateSocket();
                try
                {
                    // AcceptAsync(Args) is used here because other overloads do not accept a CancellationToken.
                    if (Socket.AcceptAsync(Args))
                        await Semaphore.WaitAsync(ct).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (ct.IsCancellationRequested)
                        yield break;
                    Logger.LogWarning(e, $"SocketAcceptor on {Socket.LocalEndPoint}. {e.Message}");
                    throw; // ??
                }

                Logger.LogTrace($"AcceptClient on {Socket.LocalEndPoint} connected to {Args.AcceptSocket.RemoteEndPoint}.");
                var client = new RxSocketClient(Args.AcceptSocket, Logger, "AcceptClient");
                AcceptedClients.Add(client);
                yield return client;
            }
        }

        internal IObservable<IRxSocketClient> CreateAcceptObservable(CancellationToken ct1)
        {
            return Observable.Create<IRxSocketClient>(async (observer, ct2) =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct1, ct2);
                var ct = cts.Token;
                try
                {
                    while (true)
                    {
                        cts.Token.ThrowIfCancellationRequested();
                        Args.AcceptSocket = Utilities.CreateSocket();
                        // AcceptAsync(Args) is used here because other overloads do not accept a CancellationToken.
                        if (Socket.AcceptAsync(Args))
                            await Semaphore.WaitAsync(ct).ConfigureAwait(false);
                        Logger.LogTrace($"AcceptClient on {Socket.LocalEndPoint} connected to {Args.AcceptSocket.RemoteEndPoint}.");
                        var client = new RxSocketClient(Args.AcceptSocket, Logger, "AcceptClient");
                        AcceptedClients.Add(client);
                        observer.OnNext(client);
                    }
                }
                catch (Exception e)
                {
                    if (ct.IsCancellationRequested)
                        observer.OnCompleted();
                    else
                    {
                        Logger.LogWarning(e, $"SocketAcceptor on {Socket.LocalEndPoint}. {e.Message}");
                        observer.OnError(e);
                    }
                }
                finally
                {
                    cts.Dispose();
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            var tasks = AcceptedClients.Select(client => client.DisposeAsync().AsTask()).ToList();
            await Task.WhenAll(tasks).ConfigureAwait(false);
            Args.Completed -= ArgsCompleted;
            Args.Dispose();
            Semaphore.Dispose();
        }
    }
}
