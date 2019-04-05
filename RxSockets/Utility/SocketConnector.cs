using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    internal static class SocketConnector
    {
        internal static async Task<(Socket, SocketError error)> ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
        {
            var socket = Utilities.CreateSocket();
            var semaphore = new SemaphoreSlim(0, 1);
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint
            };
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                ct.ThrowIfCancellationRequested();

                if (timeout == 0)
                    return (socket, SocketError.TimedOut);

                if (socket.ConnectAsync(args))
                    if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                        return (socket, SocketError.TimedOut);

                return (socket, args.SocketError);
            }
            finally
            {
                if (args.SocketError != SocketError.Success)
                {
                    if (socket.Connected)
                        Socket.CancelConnectAsync(args);
                    socket.Dispose();
                }
                args.Dispose();
            }
        }
    }
}
