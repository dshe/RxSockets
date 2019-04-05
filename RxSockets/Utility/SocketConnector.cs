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
        internal static async Task<(Socket?,Exception?)> ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
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
                if (ct.IsCancellationRequested)
                    return (null, new OperationCanceledException());

                if (timeout == 0)
                    return (null, new SocketException((int)SocketError.TimedOut));

                if (socket.ConnectAsync(args))
                    if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                        return (null, new SocketException((int)SocketError.TimedOut));

                if (args.SocketError != SocketError.Success)
                    return (null, new SocketException((int)args.SocketError));

                return (socket, null);
            }
            catch (OperationCanceledException e)
            {
                return (null, e);
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
