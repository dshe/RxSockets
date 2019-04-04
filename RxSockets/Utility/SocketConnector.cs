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
        internal static async Task<Socket> ConnectAsync(IPEndPoint endPoint, CancellationToken ct = default)
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

                if (socket.ConnectAsync(args)) // default timeout is ~20 seconds.
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                ct.ThrowIfCancellationRequested();

                if (args.SocketError != SocketError.Success)
                    throw new SocketException((int)args.SocketError);

                return args.ConnectSocket;
            }
            finally
            {
                if (args.SocketError != SocketError.Success)
                {
                    if (socket.Connected)
                        Socket.CancelConnectAsync(args);
                    socket.Dispose();
                    args.Dispose();
                    semaphore.Dispose();
                    socket.Dispose();
                }
            }
        }
    }
}
