using System.Reactive.Linq;
namespace RxSockets;

public sealed class NullRxSocketClient : IRxSocketClient
{
    public static IRxSocketClient Instance { get; } = new NullRxSocketClient();
    private NullRxSocketClient() { }
    public EndPoint RemoteEndPoint => throw new InvalidOperationException();
    public bool Connected { get; }
    public int Send(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException();
    public IObservable<byte> ReceiveObservable => Observable.Empty<byte>();
    public IAsyncEnumerable<byte> ReceiveAllAsync => throw new InvalidOperationException();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class NullRxSocketServer : IRxSocketServer
{
    public static IRxSocketServer Instance { get; } = new NullRxSocketServer();
    private NullRxSocketServer() { }
    public EndPoint LocalEndPoint => throw new InvalidOperationException();
    public IObservable<IRxSocketClient> AcceptObservable => Observable.Empty<IRxSocketClient>();
    public IAsyncEnumerable<IRxSocketClient> AcceptAllAsync => throw new InvalidOperationException();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
