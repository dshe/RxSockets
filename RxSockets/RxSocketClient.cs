using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    public interface IRxSocketClient
    {
        bool Connected { get; }
        void Send(byte[] buffer);
        void Send(byte[] buffer, int offset, int length);
        IAsyncEnumerable<byte> ReadBytesAsync();
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
        public IAsyncEnumerable<byte> ReadBytesAsync() => SocketReader.ReadBytesAsync();
        public IObservable<byte> ReceiveObservable => SocketReader.CreateReceiveObservable();
        public bool Connected =>
            !((Socket.Poll(1000, SelectMode.SelectRead) && (Socket.Available == 0)) || !Socket.Connected);

        internal RxSocketClient(Socket connectedSocket, bool isAcceptSocket, ILogger logger)
        {
            Socket = connectedSocket;
            Name = $"{(isAcceptSocket ? "Accepted " : "")}RxSocketClient";
            Logger = logger;
            Logger.LogDebug($"{Name} created on {Socket.LocalEndPoint} connected to {Socket.RemoteEndPoint}.");
            Disposer = new SocketDisposer(connectedSocket, Name, logger);
            SocketReader = new SocketReader(connectedSocket, Name, Cts.Token, logger);
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
