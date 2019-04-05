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

        private RxSocketServer(Socket socket, int backLog)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            if (socket.Connected)
                throw new SocketException((int)SocketError.IsConnected);
            Backlog = backLog > 0 ? backLog : throw new Exception($"Invalid backLog: {backLog}.");
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

        public async Task<Exception> DisconnectAsync(int timeout = -1, CancellationToken ct = default) =>
            await Disconnector.DisconnectAsync(timeout, ct).ConfigureAwait(false);

        // static!
        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));
            var socket = Utilities.CreateSocket();
            socket.Bind(endPoint);
            return new RxSocketServer(socket, backLog);
        }
    }
}
