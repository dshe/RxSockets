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
        internal Task<Exception> DisconnectAsync(CancellationToken ct = default)
        {
            lock (Socket)
            {
                return task ??= Disconnect(ct);
            }
        }

        private async Task<Exception> Disconnect(CancellationToken ct)
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
                if (Socket.Connected)
                    Socket.Shutdown(SocketShutdown.Both); // never blocks

                if (Socket.DisconnectAsync(args))
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested)
                    return new OperationCanceledException();

                return new SocketException((int)args.SocketError);
            }
            catch (Exception e)
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
