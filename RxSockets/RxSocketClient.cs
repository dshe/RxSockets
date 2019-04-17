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
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }

        internal RxSocketClient(Socket connectedSocket, ILogger logger)
        {
            Socket = connectedSocket.Connected ? connectedSocket : throw new SocketException((int)SocketError.NotConnected);
            Logger = logger;
            Disposer = new SocketDisposer(connectedSocket, logger);
            ReceiveObservable = CreateReceiveObservable();
        }

        private IObservable<byte> CreateReceiveObservable()
        {
            Logger.LogTrace($"Creating Observable.");

            var buffer = new byte[ReceiveBufferSize];

            // supports a single observer
            return Observable.Create<byte>(observer =>
            {
                Logger.LogTrace($"Observing.");

                NewThreadScheduler.Default.Schedule(() =>
                {
                    Logger.LogTrace($"Starting Receive.");

                    try
                    {
                        while (true)
                        {
                            var bytes = Socket.Receive(buffer);
                            //Logger.LogTrace($"Received {bytes} bytes.");
                            if (bytes == 0)
                                break;
                            for (int i = 0; i < bytes; i++)
                                observer.OnNext(buffer[i]);
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        // Logger.LogTrace("Receive Ended."); // crashes logger
                        if (!Disposer.DisposeRequested)
                            Logger.LogWarning(e, "Read Socket Exception.");
                        observer.OnCompleted();
                    }
                });

                return Disposable.Empty;

            });
        }

        public void Send(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                length = buffer.Length;
            //Logger.LogTrace($"Sending {length} bytes.");
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

        public void Dispose() => Disposer.Dispose();
    }
}
