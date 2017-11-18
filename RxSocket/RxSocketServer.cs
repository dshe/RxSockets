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

        private RxSocketServer(Socket socket)
        {
            Socket = socket;
            SocketAcceptor = new SocketAcceptor(socket);
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
        public static IRxSocketServer Create(int port) => Create(new IPEndPoint(IPAddress.IPv6Any, port));
        public static IRxSocketServer Create(IPEndPoint endPoint)
        {
            var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true, NoDelay = true
            };

            socket.Bind(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));

            return new RxSocketServer(socket);
        }
    }

}
