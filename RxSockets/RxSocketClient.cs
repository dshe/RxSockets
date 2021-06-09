using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RxSockets
{
    public interface IRxSocketClient: IAsyncDisposable
    {
        bool Connected { get; }
        void Send(ReadOnlySpan<byte> buffer);
        IAsyncEnumerable<byte> ReceiveAllAsync();
    }

    public class RxSocketClient: IRxSocketClient
    {
        private readonly string Name;
        private readonly ILogger Logger;
        private readonly CancellationTokenSource Cts = new();
        private readonly Socket Socket;
        private readonly SocketReceiver Receiver;
        private readonly SocketDisposer Disposer;

        internal RxSocketClient(Socket socket, ILogger logger, string name)
        {
            Socket = socket;
            Logger = logger;
            Name = name;
            Receiver = new SocketReceiver(socket, logger, Name);
            Disposer = new SocketDisposer(Socket, Cts, logger, Name);
        }

        public bool Connected =>
            !((Socket.Poll(1000, SelectMode.SelectRead) && (Socket.Available == 0)) || !Socket.Connected);

        public void Send(ReadOnlySpan<byte> buffer)
        {
            try
            {
                Socket.Send(buffer);
                Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} sent {buffer.Length} bytes to {Socket.RemoteEndPoint}.");
            }
            catch (Exception) when (Cts.IsCancellationRequested)
            {
            }
        }

        public IAsyncEnumerable<byte> ReceiveAllAsync() =>
            Receiver.ReceiveAllAsync(Cts.Token);

        public async ValueTask DisposeAsync() =>
            await Disposer.DisposeAsync().ConfigureAwait(false);
    }
}
