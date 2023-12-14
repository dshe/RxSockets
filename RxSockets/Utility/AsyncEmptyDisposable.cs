namespace RxSockets;

public sealed class AsyncEmptyDisposable : IAsyncDisposable
{
    public static IAsyncDisposable Instance { get; } = new AsyncEmptyDisposable();
    private AsyncEmptyDisposable() { }
#if NETSTANDARD2_0
    public ValueTask DisposeAsync() => new(Task.CompletedTask);
#else
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
#endif
}
