using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;

#nullable enable

namespace RxSockets
{
    public interface IRxSocketClient: IDisposable
    {
        bool Connected { get; }
        void Send(byte[] buffer, int offset = 0, int length = 0);
        IObservable<byte> ReceiveObservable { get; }
    }

    public sealed class RxSocketClient : IRxSocketClient
    {
        public static int ReceiveBufferSize = 0x1000;
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly SocketDisposer Disposer;
        private CancellationTokenSource? Cts = null;
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }

        internal RxSocketClient(Socket connectedSocket, ILogger logger)
        {
            Socket = connectedSocket.Connected ? connectedSocket : throw new SocketException((int)SocketError.NotConnected);
            Logger = logger;
            Disposer = new SocketDisposer(connectedSocket, logger);
            ReceiveObservable = CreateReceiveObservable();
            Logger.LogTrace("RxSocketClient Constructed.");
        }

        private IObservable<byte> CreateReceiveObservable()
        {
            Logger.LogTrace("Creating Observable.");

            var buffer = new byte[ReceiveBufferSize];
            int position = 0;
            var semaphore = new SemaphoreSlim(0, 1);
            void handler(object sender, SocketAsyncEventArgs a) => semaphore.Release();
            var args = new SocketAsyncEventArgs();
            args.Completed += handler;
            args.SetBuffer(buffer, 0, buffer.Length);

            return Observable.Create<byte>(observer =>
            {
                Cts = new CancellationTokenSource();

                NewThreadScheduler.Default.Schedule(() =>
                {
                    Logger.LogTrace("Starting Receive.");

                    try
                    {
                        while (!Cts!.IsCancellationRequested)
                        {
                            if (position < args.BytesTransferred)
                            {
                                observer.OnNext(buffer[position++]);
                                continue;
                            }
                            position = 0;
                            if (Socket.ReceiveAsync(args))
                                semaphore.Wait(Cts.Token);

                            Logger.LogTrace($"Received {args.BytesTransferred} bytes.");
                            if (args.BytesTransferred == 0)
                                break;
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        // Logger.LogTrace("Receive Ended."); // crashes logger
                        if (!Cts!.IsCancellationRequested && !Disposer.DisposeRequested)
                            Logger.LogWarning(e, "Read Socket Exception.");
                        observer.OnCompleted();
                    }
                });

                return Disposable.Create(() => Cts?.Cancel());

            });
        }

        public void Send(byte[] buffer, int offset = 0, int length = 0)
        {
            if (length == 0)
                length = buffer.Length;
            Logger.LogTrace($"Sending {length} bytes.");
            Socket.Send(buffer, offset, length, SocketFlags.None);
        }

        // static!
        public static Task<IRxSocketClient> ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default)
            => ConnectAsync(endPoint, NullLogger<RxSocketClient>.Instance, timeout, ct);

        public static async Task<IRxSocketClient> ConnectAsync(IPEndPoint endPoint, ILogger<RxSocketClient> logger, int timeout = -1, CancellationToken ct = default)
        {
            var socket = await SocketConnector.ConnectAsync(endPoint, logger, timeout, ct).ConfigureAwait(false);
            return new RxSocketClient(socket, logger);
        }

        public void Dispose()
        {
            Cts?.Cancel();
            Disposer.Dispose();
        }
    }
}
