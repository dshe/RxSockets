namespace RxSockets;

public interface IRxSocketClient : IAsyncDisposable
{
    EndPoint RemoteEndPoint { get; }
    bool Connected { get; }
    int Send(ReadOnlySpan<byte> buffer);
    IObservable<byte> ReceiveObservable { get; }
    IAsyncEnumerable<byte> ReceiveAllAsync { get; }
}

public sealed class RxSocketClient : IRxSocketClient
{
    private readonly string _name;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly Socket _socket;
    private readonly SocketDisposer _disposer;
    public EndPoint RemoteEndPoint { get; }
    public bool Connected =>
        !((_socket.Poll(1000, SelectMode.SelectRead) && _socket.Available == 0) || !_socket.Connected);
    public IObservable<byte> ReceiveObservable { get; }
    public IAsyncEnumerable<byte> ReceiveAllAsync { get; }

    internal RxSocketClient(Socket socket, ILogger logger, string name)
    {
        _socket = socket;
        _logger = logger;
        _name = name;
        RemoteEndPoint = _socket.RemoteEndPoint ?? throw new InvalidOperationException();
        _disposer = new SocketDisposer(socket, _name, _logger, _disposalCts);
        SocketReceiver receiver = new(socket, _name, _logger, _disposalCts.Token);
        ReceiveObservable = receiver.CreateReceiveObservable();
        ReceiveAllAsync = receiver.ReceiveAllAsync();
    }

    public int Send(ReadOnlySpan<byte> buffer)
    {
        _logger.LogSend(_name, _socket.LocalEndPoint, buffer.Length, _socket.RemoteEndPoint);
        return _socket.Send(buffer);
    }

    public async ValueTask DisposeAsync()
    {
        await _disposer.DisposeAsync().ConfigureAwait(false);
        _disposalCts.Dispose();
    }
}
