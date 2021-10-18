using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    internal class SocketDisposer : IAsyncDisposable
    {
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly IAsyncDisposable? Disposable;
        private readonly SemaphoreSlim Semaphore = new(0, 1);
        private readonly string Name;
        private readonly TaskCompletionSource<bool> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource ReceiveCts;
        private int Disposals = 0; // state
        internal bool DisposeRequested => Disposals > 0;
        private void ArgsCompleted(object? sender, SocketAsyncEventArgs e) => Semaphore.Release();

        internal SocketDisposer(Socket socket, CancellationTokenSource receiveCts, ILogger logger, string name, IAsyncDisposable? disposable = null)
        {
            Socket = socket;
            ReceiveCts = receiveCts;
            Logger = logger;
            Name = name;
            Disposable = disposable;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Increment(ref Disposals) > 1)
            {
                await Tcs.Task.ConfigureAwait(false);
                return;
            }

            try
            {
                ReceiveCts.Cancel();

                EndPoint? localEndPoint = Socket.LocalEndPoint;
                EndPoint? remoteEndPoint = Socket.RemoteEndPoint;

                if (Socket.Connected)
                {
                    // disables Send method and queues up a zero-byte send packet in the send buffer
                    Socket.Shutdown(SocketShutdown.Send);

                    SocketAsyncEventArgs args = new() { DisconnectReuseSocket = false };
                    args.Completed += ArgsCompleted;

                    if (Socket.DisconnectAsync(args))
                        await Semaphore.WaitAsync().ConfigureAwait(false);
                    Logger.LogTrace($"{Name} on {localEndPoint} disconnected from {remoteEndPoint} and disposed.");

                    args.Completed -= ArgsCompleted;
                }
                else
                {
                    Logger.LogTrace($"{Name} on {localEndPoint} disposed.");
                }

                if (Disposable is not null) // SocketAcceptor
                    await Disposable.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error on DisposeAsync().");
            }
            finally 
            {
                Tcs.SetResult(true);
                Socket.Dispose();
                ReceiveCts.Dispose();
                Semaphore.Dispose();
            }
        }
    }
}
