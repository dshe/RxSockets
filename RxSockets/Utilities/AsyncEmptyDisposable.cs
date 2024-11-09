namespace RxSockets;

public sealed class AsyncEmptyDisposable : IAsyncDisposable
{
    public static IAsyncDisposable Instance { get; } = new AsyncEmptyDisposable();
    private AsyncEmptyDisposable() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
