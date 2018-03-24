using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace RxSockets
{
    public interface IRxSocket : IDisposable
    {
        bool Connected { get; }
        void Send(byte[] buffer, int offset = 0, int length = 0);
        IObservable<byte> ReceiveObservable { get; }
        Task DisconnectAsync(CancellationToken ct = default);
    }

    public sealed class RxSocket : IRxSocket
    {
        public static int ReceiveBufferSize { get; set; } = 0x1000;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }

        private RxSocket(Socket connectedSocket)
        {
            Socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            if (!Socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);
            Disconnector = new SocketDisconnector(Socket);
            ReceiveObservable = CreateReceiveObservable();
        }

        private IObservable<byte> CreateReceiveObservable()
        {
            var buffer = new byte[ReceiveBufferSize];
            int length = 0, position = 0;

            // supports a single observer
            return Observable.Create<byte>(observer =>
            {
                var cts = new CancellationTokenSource();

                NewThreadScheduler.Default.Schedule(() =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            if (position < length)
                            {
                                observer.OnNext(buffer[position++]);
                                continue;
                            }
                            length = Socket.Receive(buffer);
                            position = 0;
                            if (length == 0)
                            {
                                observer.OnCompleted();
                                return;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (Disconnector.DisconnectRequested)
                            observer.OnCompleted();
                        else
                            observer.OnError(e);
                    }
                });
                return () => cts.Cancel();
            });
        }

        public void Send(byte[] buffer, int offset, int length) =>
            Socket.Send(buffer, offset, length > 0 ? length : buffer.Length, SocketFlags.None);

        public Task DisconnectAsync(CancellationToken ct) => Disconnector.DisconnectAsync(ct);

        // pass a cancelled token to skip waiting for disconnect
        public void Dispose() => DisconnectAsync(new CancellationToken(true)).GetAwaiter().GetResult();

        // static!
        public static IRxSocket Create(Socket connectedSocket) => new RxSocket(connectedSocket);

        public static async Task<IRxSocket> ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default) =>
            await SocketConnector.ConnectAsync(endPoint, timeout, ct);
    }

    public static class RxSocketEx
    {
        public static void SendTo(this byte[] buffer, IRxSocket rxsocket) =>
            rxsocket.Send(buffer);
    }

}
