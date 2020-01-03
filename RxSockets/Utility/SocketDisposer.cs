using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;

namespace RxSockets
{
    internal class SocketDisposer
    {
        private readonly ILogger Logger;
        private readonly Socket Socket;
        internal bool DisposeRequested => Disposed == 1;
        private int Disposed = 0;

        internal SocketDisposer(Socket socket, ILogger logger)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Logger = logger;
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref Disposed, 1, 0) == 1)
                return;

            Logger.LogDebug("Disconnecting socket.");

            try
            {
                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Both); // never blocks
                    Socket.Disconnect(false);
                    Logger.LogTrace("Socket disconnected.");
                }
                Socket.Dispose();
                Logger.LogTrace("Socket disposed.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Disconnecting socket exception.");
            }
        }

    }
}
