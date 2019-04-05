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
        private readonly TaskCompletionSource<SocketError> Tcs = new TaskCompletionSource<SocketError>();
        private int disconnectRequested = 0;
        internal bool DisconnectRequested => disconnectRequested == 1;

        internal SocketDisconnector(Socket socket) =>
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        // return Exception to enable testing
        internal async Task<SocketError> DisconnectAsync(int timeout = -1, CancellationToken ct = default)
        {
            if (Interlocked.CompareExchange(ref disconnectRequested, 1, 0) == 0)
            {
                var result = await Disconnect(timeout, ct).ConfigureAwait(false);
                Tcs.SetResult(result);
            }
            return await Tcs.Task.ConfigureAwait(false);
        }

        private async Task<SocketError> Disconnect(int timeout, CancellationToken ct)
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
                ct.ThrowIfCancellationRequested();

                if (timeout == 0)
                    return SocketError.TimedOut;

                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Both); // never blocks

                    if (Socket.DisconnectAsync(args))
                        if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                            return SocketError.TimedOut;
                }

                return args.SocketError;
            }
            finally
            {
                Socket.Dispose();
                args.Dispose();
            }
        }
    }
}
