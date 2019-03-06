using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Threading;

#nullable enable

namespace RxSockets
{
    public interface IRxSocketServer : IDisposable
    {
        IObservable<IRxSocketClient> AcceptObservable { get; }
        Task DisconnectAsync(CancellationToken ct = default);
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly int Backlog;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        private RxSocketServer(Socket socket, int backLog)
        {
            Socket = socket;
            if (socket.Connected)
                throw new SocketException((int)SocketError.IsConnected);
            Backlog = backLog;
            Disconnector = new SocketDisconnector(socket);
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

        public Task DisconnectAsync(CancellationToken ct) => Disconnector.DisconnectAsync(ct);

        // pass a cancelled token to skip waiting for disconnect
        // GetAwaiter().GetResult() is used rather then Wait()
        public void Dispose() => DisconnectAsync(new CancellationToken(true)).GetAwaiter().GetResult();

        // static!
        public static IRxSocketServer Create(int port, int backLog = 10) =>
            Create(new IPEndPoint(IPAddress.IPv6Any, port), backLog);

        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10)
        {
            var socket = NetworkHelper.CreateSocket();
            socket.Bind(endPoint);
            return Create(socket, backLog);
        }

        public static IRxSocketServer Create(Socket socket, int backLog = 10) =>
            new RxSocketServer(socket, backLog);
    }
}
