namespace RxSockets;

internal sealed class SocketDisposer : IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly Socket _socket;
    private readonly IAsyncDisposable _disposable;
    private readonly string _name;
    private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CancellationTokenSource _receiveCts;
    private int _disposals; // state
    internal bool DisposeRequested => _disposals > 0;

    internal SocketDisposer(Socket socket, string name, ILogger logger, CancellationTokenSource receiveCts) :
        this(socket, AsyncEmptyDisposable.Instance, name, logger, receiveCts)  { }
    internal SocketDisposer(Socket socket, IAsyncDisposable disposable, string name, ILogger logger, CancellationTokenSource receiveCts)
    {
        _socket = socket;
        _receiveCts = receiveCts;
        _logger = logger;
        _name = name;
        _disposable = disposable;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Increment(ref _disposals) > 1)
        {
            await _tcs.Task.ConfigureAwait(false);
            return;
        }

        try
        {
            await _receiveCts.CancelAsync().ConfigureAwait(false);

            EndPoint? localEndPoint = _socket.LocalEndPoint;
            EndPoint? remoteEndPoint = _socket.RemoteEndPoint;

            if (_socket.Connected)
            {
                // disables Send method and queues up a zero-byte send packet in the send buffer
                _socket.Shutdown(SocketShutdown.Send);
                await _socket.DisconnectAsync(false).ConfigureAwait(false);
                _logger.LogDebug("{Name} on {LocalEndPoint} disconnected from {RemoteEndPoint} and disposed.", _name, localEndPoint, remoteEndPoint);
            }
            else
            {
                _logger.LogDebug("{Name} on {LocalEndPoint} disposed.", _name, localEndPoint);
            }

            // SocketAcceptor or AsyncEmptyDisposable
            await _disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "DisposeAsync.");
        }

        try
        {
            _socket.Dispose();
            _tcs.SetResult(true);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "DisposeAsync.");
        }
    }
}
