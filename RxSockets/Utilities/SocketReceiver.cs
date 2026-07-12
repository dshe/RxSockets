using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
namespace RxSockets;

internal sealed class SocketReceiver
{
    private readonly ILogger _logger;
    private readonly Socket _socket;
    private readonly CancellationToken _receiveCt;
    private readonly string _name;
    private readonly byte[] _buffer = new byte[0x10000];
    private int _position, _bytesReceived;

    internal SocketReceiver(Socket socket, string name, ILogger logger, CancellationToken receiveCt)
    {
        _logger = logger;
        _socket = socket;
        _receiveCt = receiveCt;
        _name = name;
    }

    internal IObservable<byte> CreateReceiveObservable0()
    {
        return Observable.Create<byte>(async (observer, ct) =>
        {
            _logger.LogDebug("{Name}: SocketReceiverObservable subscribing.", _name);
            Debug.Assert(Thread.CurrentThread.IsBackground, "Not a background thread.");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_position == _bytesReceived)
                    {
                        _bytesReceived = await _socket.ReceiveAsync(_buffer, ct).ConfigureAwait(false);
                        _position = 0;

                        if (_bytesReceived == 0)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        _logger.LogReceive(_name, _socket.LocalEndPoint, _bytesReceived, _socket.RemoteEndPoint);
                    }
                    observer.OnNext(_buffer[_position++]);
                }
            }
            catch (Exception ex)
            {
                if (_receiveCt.IsCancellationRequested)
                {
                    observer.OnCompleted();
                    return;
                }
                if (ct.IsCancellationRequested)
                    return;
                _logger.LogDebug(ex, "{Name}: SocketReceiverObservable Exception: {Message}", _name, ex.Message);
                observer.OnError(ex);
            }
        });
    }

    internal IObservable<byte> CreateReceiveObservable()
    {
        return Observable.Create<byte>(async (observer, ct) =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_position >= _bytesReceived)
                    {
                        _bytesReceived = await _socket.ReceiveAsync(_buffer, ct).ConfigureAwait(false);
                        _position = 0;
                        if (_bytesReceived <= 0)
                        {
                            observer.OnCompleted();
                            return;
                        }

                        _logger.LogReceive(_name, _socket.LocalEndPoint, _bytesReceived, _socket.RemoteEndPoint);
                    }

                    observer.OnNext(_buffer[_position]);
                    _position++;
                }
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted();
            }
            catch (ObjectDisposedException)
            {
                observer.OnCompleted();
            }
            catch (SocketException) when (ct.IsCancellationRequested)
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(
                    ex,
                    "{Name}: SocketReceiverObservable Exception: {Message}",
                    _name,
                    ex.Message);

                observer.OnError(ex);
            }
        });
    }

    internal async IAsyncEnumerable<byte> ReceiveAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_position == _bytesReceived)
            {
                try
                {
                    _bytesReceived = await _socket.ReceiveAsync(_buffer, ct).ConfigureAwait(false);
                    _position = 0;
                }
                catch (Exception)
                {
                    if (ct.IsCancellationRequested || _receiveCt.IsCancellationRequested)
                        yield break;
                    throw;
                }

                if (_bytesReceived == 0)
                    yield break;

                _logger.LogReceive(_name, _socket.LocalEndPoint, _bytesReceived, _socket.RemoteEndPoint);
            }
            yield return _buffer[_position++];
        }
    }
}
