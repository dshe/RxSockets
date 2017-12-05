using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Threading;

namespace RxSockets
{
    public interface IRxSocketServer : IDisposable
    {
        IObservable<IRxSocket> AcceptObservable { get; }
        Task DisconnectAsync(CancellationToken ct = default);
    }

    public sealed class RxSocketServer : IRxSocketServer
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        public static int Backlog { get; set; } = 10;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public IObservable<IRxSocket> AcceptObservable { get; }
        private int Listening;

        private RxSocketServer(Socket socket)
        {
            Socket = socket;
            Disconnector = new SocketDisconnector(socket);
            AcceptObservable = CreateAcceptObservable();
        }

        private IObservable<IRxSocket> CreateAcceptObservable()
        {
            // supports a single observer
            return Observable.Create<IRxSocket>(observer =>
            {
                if (Interlocked.CompareExchange(ref Listening, 1, 0) == 0)
                    Socket.Listen(Backlog);

                var cts = new CancellationTokenSource();

                NewThreadScheduler.Default.Schedule(() =>
                {
                    try
                    {
                        while (!cts.IsCancellationRequested)
                            observer.OnNext(new RxSocket(Socket.Accept()));
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

        public Task DisconnectAsync(CancellationToken ct) => Disconnector.DisconnectAsync(ct);

        // pass a cancelled token to skip waiting for disconnect
        public void Dispose() =>
            Disconnector.DisconnectAsync(new CancellationToken(true)).GetAwaiter().GetResult();


        // static!
        public static IRxSocketServer Create(int port) =>
            Create(new IPEndPoint(IPAddress.IPv6Any, port));
        public static IRxSocketServer Create(IPEndPoint endPoint)
        {
            var socket = NetworkUtility.CreateSocket();

            socket.Bind(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));

            return new RxSocketServer(socket);
        }
    }

}
