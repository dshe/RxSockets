using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;

#nullable enable

namespace RxSockets
{
    internal class SocketDisposer
    {
        private readonly ILogger Logger;
        private readonly Socket Socket;
        internal bool DisposeRequested { get; private set; }

        internal SocketDisposer(Socket socket, ILogger logger)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Logger = logger;
        }

        public void Dispose()
        {
            Logger.LogDebug("Disconnecting socket.");
            lock (this)
            {
                if (DisposeRequested)
                    return;
               DisposeRequested = true;
               DisposeImpl();
            }
        }

        private void DisposeImpl()
        {
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
