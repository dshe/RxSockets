using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    public interface IRxSocketClient
    {
        bool Connected { get; }
        void Send(byte[] buffer);
        void Send(byte[] buffer, int offset, int length);
        Task<byte> ReadAsync();
        IObservable<byte> ReceiveObservable { get; }
        Task DisposeAsync();
    }

    public sealed class RxSocketClient : IRxSocketClient
    {
        private readonly ILogger Logger;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private readonly string Name;
        private readonly Socket Socket;
        private readonly SocketDisposer Disposer;
        private readonly SocketReader SocketReader;
        public Task<byte> ReadAsync() => SocketReader.ReadAsync();
        public IObservable<byte> ReceiveObservable { get; }
        public bool Connected => Socket.Connected;

        internal RxSocketClient(Socket connectedSocket, bool isAcceptSocket, ILogger logger)
        {
            Socket = connectedSocket;
            Name = $"{(isAcceptSocket ? "Accepted " : "")}RxSocketClient";
            Logger = logger;
            Disposer = new SocketDisposer(connectedSocket, Name, logger);
            SocketReader = new SocketReader(connectedSocket, Name, Cts.Token, logger);
            ReceiveObservable = SocketReader.Read()
                .ToObservable(NewThreadScheduler.Default.Catch<Exception>(ExceptionHandler));
            Logger.LogDebug($"{Name} created on {Socket.LocalEndPoint} connected to {Socket.RemoteEndPoint}.");
        }

        private bool ExceptionHandler(Exception e)
        {
            if (!Cts.IsCancellationRequested)
                Logger.LogTrace($"{Name} scheduler caught {e.ToString()}.");
            return true;
        }


        public void Send(byte[] buffer) => Send(buffer, 0, buffer.Length);
        public void Send(byte[] buffer, int offset, int length)
        {
            Socket.Send(buffer, offset, length, SocketFlags.None);
            Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} sent {length} bytes to {Socket.RemoteEndPoint}.");
        }

        public async Task DisposeAsync()
        {
            Cts.Cancel();
            await Disposer.DisposeAsync().ConfigureAwait(false);
        }
    }
}
