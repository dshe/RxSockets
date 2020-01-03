using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RxSockets
{
    class SocketAcceptReader
    {
        private readonly ILogger Logger;
        private readonly CancellationToken Ct;
        private readonly Socket Socket;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();

        internal SocketAcceptReader(Socket socket, ILogger logger, CancellationToken ct)
        {
            Socket = socket;
            Logger = logger;
            Ct = ct;
            Args.Completed += (x, y) => Semaphore.Release();
        }

        internal IEnumerable<Socket> Read()
        {
            Logger.LogTrace("Accept started.");
            while (true)
            {
                Args.AcceptSocket = Utilities.CreateSocket();
                if (Socket.AcceptAsync(Args))
                     Semaphore.Wait(Ct);
                Logger.LogInformation($"Accepted socket: {Args.AcceptSocket.LocalEndPoint}.");
                yield return Args.AcceptSocket;
            }
        }
    }
}
