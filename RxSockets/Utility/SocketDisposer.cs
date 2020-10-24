using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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
            Socket = socket;
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
                    // disables Send and Receive methods and queues up a zero-byte send packet in the send buffer
                    Socket.Shutdown(SocketShutdown.Both);

                    var remoteEndPoint = Socket.RemoteEndPoint;
                    var semaphore = new SemaphoreSlim(0, 1);
                    var args = new SocketAsyncEventArgs
                    {
                        DisconnectReuseSocket = false
                    };
                    args.Completed += (_, __) => semaphore.Release();
                    if (Socket.DisconnectAsync(args))
                        await semaphore.WaitAsync().ConfigureAwait(false);
                    Logger.LogTrace($"{Name} on {localEndPoint} disconnected from {remoteEndPoint} and disposed.");
                }
                else
                {
                    Logger.LogTrace($"{Name} on {localEndPoint} disposed.");
                }
                Tcs.SetResult(true);
            }
            catch (Exception e)
            {
                Tcs.SetException(e);
            }
            finally
            {
                Socket.Dispose();
            }
        }
    }
}
