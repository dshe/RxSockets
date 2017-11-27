using System;
using System.Diagnostics;
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
                else if (ct.IsCancellationRequested)
                    return CancelAndDispose(SocketError.OperationAborted);

                if (args.SocketError == SocketError.Success)
                    return (SocketError.Success, new RxSocket(args.ConnectSocket));

                return CancelAndDispose(args.SocketError);
            }
            catch (OperationCanceledException)
            {
                return CancelAndDispose(SocketError.OperationAborted);
            }
            catch (SocketException se)
            {
                return CancelAndDispose(se.SocketErrorCode);
            }
            catch (ObjectDisposedException)
            {
                return CancelAndDispose(SocketError.Shutdown);
            }
            catch (Exception e)
            {
                Debug.WriteLine("SocketConnector Unhandled Exception: " + e.Message);
                throw;
            }

            // local
            (SocketError, IRxSocket) CancelAndDispose(SocketError error)
            {
                Socket.CancelConnectAsync(args);
                socket.Dispose();
                return (error, null);
            }

        }
    }
}
