using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reactive.Disposables;
using RxSockets;
using System.Collections.Generic;

namespace RxSockets
{
    public interface IRxSocketClient : IDisposable
    {
        bool Connected { get; }
        void Send(byte[] buffer, int offset, int length);
        void Send(byte[] buffer);
        IObservable<byte> ReceiveObservable { get; }
    }

    public sealed class RxSocketClient : IRxSocketClient
    {
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly SocketDisposer Disposer;
        private readonly CancellationTokenSource Cts = new CancellationTokenSource();
        public bool Connected => Socket.Connected;
        public IObservable<byte> ReceiveObservable { get; }
        private readonly SocketReader SocketReader;

        internal RxSocketClient(Socket connectedSocket, ILogger logger)
        {
            Socket = connectedSocket.Connected ? connectedSocket : throw new SocketException((int)SocketError.NotConnected);
            Logger = logger;
            Disposer = new SocketDisposer(connectedSocket, logger);
            SocketReader = new SocketReader(connectedSocket, Cts.Token, logger);
            ReceiveObservable = SocketReader.Read().ToObservable(NewThreadScheduler.Default);
            Logger.LogTrace("RxSocketClient constructed.");
        }

        public Task<byte> ReadAsync() => SocketReader.ReadAsync();

        public void Send(byte[] buffer) => Send(buffer, 0, buffer.Length);
        public void Send(byte[] buffer, int offset, int length)
        {
            Socket.Send(buffer, offset, length, SocketFlags.None);
            Logger.LogTrace($"Sent {length} bytes.");
        }

        public void Dispose()
        {
            Cts.Cancel();
            Disposer.Dispose();
        }
    }
  
}
