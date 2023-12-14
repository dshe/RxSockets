namespace RxSockets;

public sealed class NullRxSocketClient : IRxSocketClient
{
    public static NullRxSocketClient Instance { get; } = new NullRxSocketClient();
    private NullRxSocketClient() { }
    public EndPoint RemoteEndPoint => throw new InvalidOperationException();
    public bool Connected { get; }
    public int Send(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException();
    public IAsyncEnumerable<byte> ReceiveAllAsync => throw new InvalidOperationException();
#if NETSTANDARD2_0
    public ValueTask DisposeAsync() => new(Task.CompletedTask);
#else
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
#endif
}
