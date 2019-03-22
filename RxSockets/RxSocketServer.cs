using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    public interface IRxSocketServer: IAsyncDisconnectable
    {
        IObservable<IRxSocketClient> AcceptObservable { get; }
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly int Backlog;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        private RxSocketServer(Socket socket, int backLog, CancellationToken ct)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            if (socket.Connected)
                throw new SocketException((int)SocketError.IsConnected);
            Backlog = backLog;
            Disconnector = new SocketDisconnector(socket, ct);
            AcceptObservable = CreateAcceptObservable();
        }

        private IObservable<IRxSocketClient> CreateAcceptObservable()
        {
            // supports a single observer
            return Observable.Create<IRxSocketClient>(observer =>
            {
                return NewThreadScheduler.Default.Schedule(() =>
                {
                    try
                    {
                        Socket.Listen(Backlog);

                        while (true)
                            observer.OnNext(RxSocketClient.Create(Socket.Accept()));
                    }
                    catch (Exception e)
                    {
                        if (Disconnector.DisconnectRequested)
                            observer.OnCompleted();
                        else
                            observer.OnError(e);
                    }
                });
            });
        }

        public async Task DisconnectAsync() => await Disconnector.DisconnectAsync().ConfigureAwait(false);

        // static!
        public static IRxSocketServer Create(int port, int backLog = 10, CancellationToken ct = default) =>
            Create(new IPEndPoint(IPAddress.IPv6Any, port), backLog, ct);

        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10, CancellationToken ct = default)
        {
            var socket = NetworkHelper.CreateSocket();
            socket.Bind(endPoint);
            return Create(socket, backLog, ct);
        }

        public static IRxSocketServer Create(Socket socket, int backLog = 10, CancellationToken ct = default) =>
            new RxSocketServer(socket, backLog, ct);
    }
}
