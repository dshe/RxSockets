using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

[assembly: InternalsVisibleTo("RxSocket.Tests")]

namespace RxSocket
{
    public interface IRxSocket
    {
        bool Connected { get; }
        void Send(byte[] buffer, int offset = 0, int length = 0);
        IObservable<byte> ReceiveObservable { get; }
        Task<SocketError> DisconnectAsync(int timeout = -1);
    }

    public class RxSocket : IRxSocket
    {
        public static int ReceiveBufferSize { get; set; } = 0x1000;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }

        internal RxSocket(Socket connectedSocket)
        {
            Socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));
            if (!Socket.Connected)
                throw new SocketException((int)SocketError.NotConnected);
            Disconnector = new SocketDisconnector(connectedSocket);

            ReceiveObservable = Observable.Create<byte>(observer =>
            {
                NewThreadScheduler.Default.Schedule(() =>
                {
                    try
                    {
                        var buffer = new byte[ReceiveBufferSize];

                        while (true)
                        {
                            int length = Socket.Receive(buffer);
                            if (length == 0)
                                break;
                            for (int i = 0; i < length; i++)
                                observer.OnNext(buffer[i]);
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        //if (Disconnector.DisconnectRequested)
                        //    observer.OnCompleted();
                        //else
                            observer.OnError(e);
                    }
                });
                return Disposable.Empty;
            });
        }

        public void Send(byte[] buffer, int offset, int length)
        {
            try
            {
                Socket.Send(buffer, offset, length > 0 ? length : buffer.Length, SocketFlags.None);
            }
            catch (Exception)
            {
                if (Disconnector.DisconnectRequested)
                    throw new SocketException((int)SocketError.Disconnecting);
                throw;
            }
        }

        public Task<SocketError> DisconnectAsync(int timeout) => Disconnector.DisconnectAsync(timeout);

        // static!
        public static async Task<(SocketError error, IRxSocket rxsocket)>
            ConnectAsync(IPEndPoint endPoint, int timeout = -1, CancellationToken ct = default) =>
                await SocketConnector.ConnectAsync(endPoint, timeout, ct);
    }

    public static class XSocketEx
    {
        public static void SendTo(this byte[] buffer, IRxSocket rxsocket) => rxsocket.Send(buffer);
    }

}
