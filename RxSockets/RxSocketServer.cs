using System;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
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
        private readonly Socket Socket;
        private readonly SocketAccepter SocketAccepter;
        private readonly List<RxSocketClient> Clients = new List<RxSocketClient>();
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        internal RxSocketServer(Socket socket, ILogger logger)
        {
            Socket = socket;
            Logger = logger;
            Logger.LogDebug($"RxSocketServer created on {socket.LocalEndPoint}.");
            SocketAccepter = new SocketAccepter(socket, Cts.Token, Logger);
            AcceptObservable = SocketAccepter.CreateAcceptObservable()
                .Select(acceptSocket => new RxSocketClient(acceptSocket, true, logger))
                .Do(client => Clients.Add(client));
        }

        public async Task DisposeAsync()
        {
            Cts.Cancel();
            await Task.WhenAll(Clients.Select(client => client.DisposeAsync())).ConfigureAwait(false);
            Socket.Close();
        }
    }
}
