using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace RxSocket
{
    public interface IRxSocketServer
    {
        IObservable<IRxSocket> AcceptObservable { get; }
        Task<SocketError> DisconnectAsync(int timeout = -1);
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        private readonly Socket Socket;
        private readonly SocketAcceptor SocketAcceptor;
        private readonly SocketDisconnector Disconnector;
        public IObservable<IRxSocket> AcceptObservable { get; }

        private RxSocketServer(Socket socket, int backlog)
        {
            Socket = socket;
            SocketAcceptor = new SocketAcceptor(socket, backlog);
            Disconnector = new SocketDisconnector(socket);

            AcceptObservable = Observable.Create<IRxSocket>(observer =>
            {
                NewThreadScheduler.Default.Schedule(() =>
                {
                    try
                    {
                        while (true)
                        {
                            (SocketError error, Socket acceptSocket) = SocketAcceptor.Accept();
                            if (error != SocketError.Success)
                            {
                                if (Disconnector.DisconnectRequested)
                                    observer.OnCompleted();
                                else
                                    observer.OnError(new SocketException((int)error));
                                return;
                            }
                            observer.OnNext(new RxSocket(acceptSocket));
                        }
                    }
                    catch (Exception e)
                    {
                        observer.OnError(e);
                    }
                });
                return Disposable.Empty;
            });

        }

        public Task<SocketError> DisconnectAsync(int timeout) => Disconnector.DisconnectAsync(timeout);

        // static!
        public static IRxSocketServer Create(int port, int backlog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Any, port), backlog);
        public static IRxSocketServer Create(IPEndPoint endPoint, int backlog = 10)
        {
            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true, NoDelay = true
            };

            socket.Bind(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));

            if (backlog < 0)
                throw new ArgumentOutOfRangeException(nameof(backlog));

            return new RxSocketServer(socket, backlog);
        }
    }

}
