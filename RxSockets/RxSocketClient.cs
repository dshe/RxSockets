using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace RxSockets;

public interface IRxSocketClient : IAsyncDisposable
{
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

    internal RxSocketClient(Socket socket, ILogger logger, string name)
    {
        Socket = socket;
        Logger = logger;
        Name = name;
        Receiver = new SocketReceiver(socket, Logger, Name);
        Disposer = new SocketDisposer(Socket, ReceiveCts, Logger, Name);
    }

    public bool Connected =>
        !((Socket.Poll(1000, SelectMode.SelectRead) && (Socket.Available == 0)) || !Socket.Connected);

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
