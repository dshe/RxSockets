namespace RxSockets;

public interface IRxSocketClient : IAsyncDisposable
{
    EndPoint RemoteEndPoint { get; }
    bool Connected { get; }
    int Send(ReadOnlySpan<byte> buffer);
    IAsyncEnumerable<byte> ReceiveAllAsync { get; }
}

public sealed class RxSocketClient : IRxSocketClient
{
    private readonly string Name;
    private readonly ILogger Logger;
    private readonly CancellationTokenSource ReceiveCts = new();
    private readonly Socket Socket;
    private readonly SocketDisposer Disposer;
    public EndPoint RemoteEndPoint { get; }
    public bool Connected =>
        !((Socket.Poll(1000, SelectMode.SelectRead) && Socket.Available == 0) || !Socket.Connected);
    public IAsyncEnumerable<byte> ReceiveAllAsync { get; }

    internal RxSocketClient(Socket socket, ILogger logger, string name)
    {
        Socket = socket;
        Logger = logger;
        Name = name;
        RemoteEndPoint = Socket.RemoteEndPoint ?? throw new InvalidOperationException();
        Disposer = new SocketDisposer(socket, Name, ReceiveCts, Logger);
        SocketReceiver receiver = new(socket, Name, Logger);
        ReceiveAllAsync = receiver.ReceiveAllAsync(ReceiveCts.Token);
    }

    public int Send(ReadOnlySpan<byte> buffer)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("{Name} on {LocalEndPoint} sending {Bytes} bytes to {RemoteEndPoint}.", Name, Socket.LocalEndPoint, buffer.Length, Socket.RemoteEndPoint);
        return Socket.Send(buffer);
    }

    public async ValueTask DisposeAsync()
    {
        await Disposer.DisposeAsync().ConfigureAwait(false);
        ReceiveCts.Dispose();
    }
}
