using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace RxSockets;

public interface IRxSocketClient : IAsyncDisposable
{
    IPEndPoint RemoteIPEndPoint { get; }
    bool Connected { get; }
    int Send(ReadOnlySpan<byte> buffer);
    IAsyncEnumerable<byte> ReceiveAllAsync();
}

public sealed class RxSocketClient : IRxSocketClient
{
    private readonly string Name;
    private readonly ILogger Logger;
    private readonly CancellationTokenSource ReceiveCts = new();
    private readonly Socket Socket;
    private readonly SocketReceiver Receiver;
    private readonly SocketDisposer Disposer;
    public IPEndPoint RemoteIPEndPoint => Socket.RemoteEndPoint as IPEndPoint ?? throw new InvalidOperationException();
    public bool Connected =>
        !((Socket.Poll(1000, SelectMode.SelectRead) && (Socket.Available == 0)) || !Socket.Connected);

    internal RxSocketClient(Socket socket, ILogger logger, string name)
    {
        Socket = socket;
        Logger = logger;
        Name = name;
        Receiver = new SocketReceiver(socket, Logger, Name);
        Disposer = new SocketDisposer(socket, ReceiveCts, Logger, Name);
    }

    public int Send(ReadOnlySpan<byte> buffer)
    {
        int bytes = Socket.Send(buffer);
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{Name} on {LocalEndPoint} sent {Bytes} bytes to {RemoteEndPoint}.", Name, Socket.LocalEndPoint, bytes, Socket.RemoteEndPoint);
        return bytes;
    }

    public IAsyncEnumerable<byte> ReceiveAllAsync() => Receiver.ReceiveAllAsync(ReceiveCts.Token);

    public async ValueTask DisposeAsync()
    {
        await Disposer.DisposeAsync().ConfigureAwait(false);
        ReceiveCts.Dispose();
    }
}
