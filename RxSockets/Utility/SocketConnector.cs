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
        internal static async Task<IRxSocket> ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
        {
            var socket = NetworkHelper.CreateSocket();

            var semaphore = new SemaphoreSlim(0, 1);

            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint
            };
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                if (socket.ConnectAsync(args)) // default timeout is ~20 seconds.
                {
                    if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                        throw new SocketException((int)SocketError.TimedOut);
                }
                else
                    ct.ThrowIfCancellationRequested();

                if (args.SocketError != SocketError.Success)
                    throw new SocketException((int)args.SocketError);

                return RxSocketClient.Create(args.ConnectSocket);
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
