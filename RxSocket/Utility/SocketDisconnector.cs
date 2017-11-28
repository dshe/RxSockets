using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace RxSocket
{
    internal class SocketDisconnector
    {
        private readonly Socket Socket;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly TaskCompletionSource<Exception> Tcs = new TaskCompletionSource<Exception>();
        private int disconnect;
        internal bool DisconnectRequested => disconnect == 1;

        internal SocketDisconnector(Socket socket) =>
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));

        internal async Task<Exception> DisconnectAsync(CancellationToken ct = default)
        {
            using (var registration = ct.Register(Cts.Cancel))
            {
                if (Interlocked.CompareExchange(ref disconnect, 1, 0) == 0)
                    Tcs.SetResult(await Disconnect(Socket, Cts.Token));
                return await Tcs.Task;
            }
        }

        private static async Task<Exception> Disconnect(Socket socket, CancellationToken ct)
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

                return new SocketException((int)args.SocketError);
            }
            catch (Exception e)
            {
                return e;
            }
            finally
            {
                socket.Dispose();
            }
        }

    }
}
