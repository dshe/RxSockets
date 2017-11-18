using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSocket
{
    internal class SocketDisconnector
    {
        private readonly Socket Socket;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly TaskCompletionSource<SocketError> Tcs = new TaskCompletionSource<SocketError>();
        private int disconnect;
        internal bool DisconnectRequested => disconnect == 1;

        internal SocketDisconnector(Socket socket) =>
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        internal async Task<SocketError> DisconnectAsync(int timeout = -1)
        {
            if (timeout == 0)
                Cts.Cancel();
            else if (timeout > 0)
                Cts.CancelAfter(timeout);

            if (Interlocked.CompareExchange(ref disconnect, 1, 0) == 0)
                Tcs.SetResult(await Disconnect(Socket, Cts.Token));

            return await Tcs.Task;
        }

        private static async Task<SocketError> Disconnect(Socket socket, CancellationToken ct)
        {
            Debug.WriteLine("Disconnecting socket.");

            var args = new SocketAsyncEventArgs
            {
                DisconnectReuseSocket = false
            };

            var semaphore = new SemaphoreSlim(0, 1);
            args.Completed += (sender, a) => semaphore.Release();

            try
            {
                if (socket.Connected)
                    socket.Shutdown(SocketShutdown.Both);

                if (socket.DisconnectAsync(args))
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);
                else
                    ct.ThrowIfCancellationRequested();

                return args.SocketError;
            }
            catch (OperationCanceledException)
            {
                // cancellation was actually caused by timeout
                return SocketError.TimedOut;
            }
            catch (SocketException se)
            {
                return se.SocketErrorCode;
            }
            catch (ObjectDisposedException)
            {
                return SocketError.Shutdown;
            }
            catch (Exception e)
            {
                Debug.WriteLine("SocketDisconnector unhandled exception: " + e.Message);
                throw;
            }
            finally
            {
                socket.Dispose();
            }
        }
    }
}
