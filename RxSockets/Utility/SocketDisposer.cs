using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    internal class SocketDisposer
    {
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly string Name;
        internal bool DisposeRequested => Disposed == 1;
        private int Disposed = 0;
        private readonly TaskCompletionSource<bool> Tcs = new TaskCompletionSource<bool>();

        internal SocketDisposer(Socket socket, string name, ILogger logger)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Name = name;
            Logger = logger;
        }

        internal async Task DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref Disposed, 1, 0) == 1)
            {
                await Tcs.Task.ConfigureAwait(false);
                return;
            }

            try
            {
                var localEndPoint = Socket.LocalEndPoint;
                if (Socket.Connected)
                {
                    var remoteEndPoint = Socket.RemoteEndPoint;
                    var semaphore = new SemaphoreSlim(0, 1);
                    var args = new SocketAsyncEventArgs
                    {
                        DisconnectReuseSocket = false
                    };
                    args.Completed += (_, __) => semaphore.Release();
                    if (Socket.DisconnectAsync(args))
                        await semaphore.WaitAsync().ConfigureAwait(false);
                    Logger.LogDebug($"{Name} on {localEndPoint} disposed and disconnected from {remoteEndPoint}.");
                }
                else
                {
                    Socket.Dispose();
                    Logger.LogDebug($"{Name} on {localEndPoint} disposed.");
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, $"{Name} dispose exception.");
                throw;
            }
            finally
            {
                Tcs.SetResult(true);
            }
        }
    }
}
