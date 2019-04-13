using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#nullable enable

namespace RxSockets
{
    public interface IRxSocketClient: IAsyncDisconnectable
    {
        bool Connected { get; }
        void Send(byte[] buffer, int offset = 0, int length = 0);
        IObservable<byte> ReceiveObservable { get; }
    }

    public sealed class RxSocketClient : IRxSocketClient
    {
        public static int ReceiveBufferSize { get; set; } = 0x1000;
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }

        private RxSocketClient(Socket connectedSocket, ILogger logger)
        {
            Logger = logger;
            Socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            if (!Socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);
            Disconnector = new SocketDisconnector(Socket, logger);
            ReceiveObservable = CreateReceiveObservable();
        }

        private IObservable<byte> CreateReceiveObservable()
        {
            // supports a single observer
            return Observable.Create<byte>(observer =>
            {
                var cts = new CancellationTokenSource();

                NewThreadScheduler.Default.Schedule(() =>
                {
                    var semaphore = new SemaphoreSlim(0, 1);
                    void handler(object sender, SocketAsyncEventArgs a) => semaphore.Release();
                    var args = new SocketAsyncEventArgs();
                    args.Completed += handler;
                    var buffer = new byte[ReceiveBufferSize];
                    args.SetBuffer(buffer, 0, buffer.Length);
                    Logger.LogTrace($"Start Receive.");

                    try
                    {
                        while (true)
                        {
                            if (Socket.ReceiveAsync(args))
                                semaphore.Wait(cts.Token);
                            else if (cts.IsCancellationRequested)
                            {
                                observer.OnCompleted();
                                return;
                            }

                            Logger.LogTrace($"Received {args.BytesTransferred} bytes.");
                            if (args.BytesTransferred == 0)
                            {
                                observer.OnCompleted();
                                return;
                            }
                            for (int i=0; i < args.BytesTransferred; i++)
                                observer.OnNext(buffer[i]);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        if (Disconnector.DisconnectRequested)
                            observer.OnCompleted();
                        else
                        {
                            Logger.LogTrace(e, "Read Socket Exception.");
                            observer.OnError(e);
                        }
                    }
                    finally
                    {
                        args.Completed -= handler;
                        args.Dispose();
                        semaphore.Dispose();
                        if (cts.IsCancellationRequested)
                            Logger.LogTrace("Receive Cancelled.");
                        else
                            Logger.LogTrace("Receive Ended.");
                    }
                });

                return () => cts.Cancel();
            });
        }

        public void Send(byte[] buffer, int offset, int length)
        {
            if (length == 0)
                length = buffer.Length;
            Logger.LogTrace($"Sending {length} bytes.");
            Socket.Send(buffer, offset, length, SocketFlags.None);
        }

        public async Task<SocketError> DisconnectAsync(int timeout = 1000, CancellationToken ct = default) =>
            await Disconnector.DisconnectAsync(timeout, ct).ConfigureAwait(false);

        // static!
        public static Task<(IRxSocketClient?, SocketError)> ConnectAsync(IPEndPoint endPoint, int timeout = 1000, CancellationToken ct = default)
            => ConnectAsync(endPoint, NullLoggerFactory.Instance, timeout, ct);
        public static async Task<(IRxSocketClient?, SocketError)> ConnectAsync(IPEndPoint endPoint, ILoggerFactory loggerFactory, int timeout = 1000, CancellationToken ct = default)
        {
            var logger = loggerFactory.CreateLogger<RxSocketClient>();
            logger.LogInformation($"Connecting to EndPoint: {endPoint}.");
            (Socket socket, SocketError result) = await SocketConnector.ConnectAsync(endPoint, timeout, ct).ConfigureAwait(false);
            if (result != SocketError.Success)
            {
                logger.LogInformation($"'{result}'. Could not connect to EndPoint: {endPoint}.");
                return (null, result);
            }
            return (Create(socket, logger), result);
        }

        internal static IRxSocketClient Create(Socket connectedSocket, ILogger logger) =>
            new RxSocketClient(connectedSocket, logger);
    }
}
