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
            ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
        {
            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true, NoDelay = true
            };

            var semaphore = new SemaphoreSlim(0, 1);
            var s = semaphore;
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint))
            };
            void Handler(object _, SocketAsyncEventArgs a) => s.Release();
            args.Completed += Handler;

            try
            {
                if (socket.ConnectAsync(args)) // default timeout is ~20 seconds.
                {
                    if (!await s.WaitAsync(timeout, ct).ConfigureAwait(false))
                        return CancelAndDispose(SocketError.TimedOut);
                }
                else
                    ct.ThrowIfCancellationRequested();

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
            finally
            {
                args.Completed -= Handler;
                args.Dispose();
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
