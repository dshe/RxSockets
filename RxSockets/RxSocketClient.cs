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
                    try
                    {
                        Logger.LogTrace($"Start Receive.");
                        var buffer = new byte[ReceiveBufferSize];

                        while (!cts.IsCancellationRequested)
                        {
                            var length = Socket.Receive(buffer);
                            if (length == 0)
                            {
                                observer.OnCompleted();
                                return;
                            }
                            Logger.LogTrace($"Received {length} bytes.");
                            for (int i=0; i < length; i++)
                                observer.OnNext(buffer[i]);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogTrace(e, "Read Socket Exception.");
                        if (Disconnector.DisconnectRequested)
                            observer.OnCompleted();
                        else
                            observer.OnError(e);
                    }
                    finally
                    {
                        Logger.LogTrace("End Receive.");
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
