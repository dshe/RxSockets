using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    internal static class SocketConnector
    {
        internal static async Task<(SocketError error, IRxSocket socket)> 
            TryConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
        {
            var socket = NetworkHelper.CreateSocket();

            var semaphore = new SemaphoreSlim(0, 1);

            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint))
            };
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                if (socket.ConnectAsync(args)) // default timeout is ~20 seconds.
                {
                    if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                        return (SocketError.TimedOut, null);
                }
                else
                    ct.ThrowIfCancellationRequested();

                return (args.SocketError, args.SocketError == SocketError.Success ? RxSocket.Create(args.ConnectSocket) : null);
            }
            finally
            {
                if (args.SocketError != SocketError.Success)
                {
                    Socket.CancelConnectAsync(args);
                    socket.Dispose();
                }
            }
        }
    }
}
