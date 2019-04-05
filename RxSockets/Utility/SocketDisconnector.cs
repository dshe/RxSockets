using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    internal class SocketDisconnector
    {
        private readonly Socket Socket;
        private Task<Exception>? task = null;
        internal bool DisconnectRequested => task != null;
        internal SocketDisconnector(Socket socket)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        // return Exception to enable testing
        internal Task<Exception> DisconnectAsync(int timeout = -1, CancellationToken ct = default)
        {
            lock (Socket)
            {
                return task ??= Disconnect(timeout, ct);
            }
        }

        private async Task<Exception> Disconnect(int timeout, CancellationToken ct)
        {
            Debug.WriteLine("Disconnecting socket.");

            var args = new SocketAsyncEventArgs()
            {
                DisconnectReuseSocket = false
            };

            var semaphore = new SemaphoreSlim(0, 1);
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                if (ct.IsCancellationRequested)
                    return new OperationCanceledException();

                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Both); // never blocks

                    if (Socket.DisconnectAsync(args))
                        if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                            return new SocketException((int)SocketError.TimedOut);

                    if (ct.IsCancellationRequested)
                        return new OperationCanceledException();
                }

                return new SocketException((int)args.SocketError);
            }
            catch (OperationCanceledException e)
            {
                return e;
            }
            finally
            {
                Socket.Dispose();
                args.Dispose();
                semaphore.Dispose();
            }
        }
    }
}
