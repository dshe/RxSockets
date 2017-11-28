using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSocket
{
    internal static class SocketConnector
    {
        internal static async Task<(SocketError error, IRxSocket socket)> 
            ConnectAsync(IPEndPoint endPoint, CancellationToken ct = default)
        {
            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true, NoDelay = true
            };

            var semaphore = new SemaphoreSlim(0, 1);

            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint))
            };
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                if (socket.ConnectAsync(args)) // default timeout is ~20 seconds.
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);
                else
                    ct.ThrowIfCancellationRequested();

                return (args.SocketError, args.SocketError == SocketError.Success ? new RxSocket(args.ConnectSocket) : null);
            }
            catch (SocketException se)
            {
                return (se.SocketErrorCode, null);
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
