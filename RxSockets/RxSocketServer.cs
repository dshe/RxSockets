using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
        private readonly ILogger Logger;
        private readonly int Backlog;
        private readonly Socket Socket;
        private readonly SocketDisconnector Disconnector;
        public IObservable<IRxSocketClient> AcceptObservable { get; }

        private RxSocketServer(Socket socket, int backLog, ILogger logger)
        {
            Logger = logger;
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            if (socket.Connected)
                throw new SocketException((int)SocketError.IsConnected);
            Backlog = backLog > 0 ? backLog : throw new Exception($"Invalid backLog: {backLog}.");
            Disconnector = new SocketDisconnector(socket, logger);
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
                        Logger.LogInformation($"Listening.");
                        Socket.Listen(Backlog);

                        while (true)
                        {
                            var accepted = Socket.Accept();
                            Logger.LogInformation($"Accepted client: {accepted.LocalEndPoint}.");
                            var rxsocket = RxSocketClient.Create(accepted, Logger);
                            observer.OnNext(rxsocket);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogInformation(e, "Exception.");
                        if (Disconnector.DisconnectRequested)
                            observer.OnCompleted();
                        else
                            observer.OnError(e);
                    }
                });
            });
        }

        public async Task<SocketError> DisconnectAsync(int timeout = -1, CancellationToken ct = default) =>
            await Disconnector.DisconnectAsync(timeout, ct).ConfigureAwait(false);

        public static IRxSocketServer Create(IPEndPoint endPoint, int backLog = 10) =>
            Create(endPoint, NullLoggerFactory.Instance, backLog);
        public static IRxSocketServer Create(IPEndPoint endPoint, ILoggerFactory loggerFactory, int backLog = 10)
        {
            var logger = loggerFactory.CreateLogger<RxSocketServer>();
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint));
            var socket = Utilities.CreateSocket();
            logger.LogInformation($"Creating server at EndPoint: {endPoint}.");
            socket.Bind(endPoint);
            return new RxSocketServer(socket, backLog, logger);
        }
    }
}
