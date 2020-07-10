using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    internal class SocketReader
    {
        public const int BufferLength = 0x1000;
        private readonly byte[] Buffer = new byte[BufferLength];
        private readonly ILogger Logger;
        private readonly CancellationToken Ct_disposed;
        private readonly Socket Socket;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();
        private readonly string Name;
        private int Position;
        internal IObservable<byte> ReceiveObservable { get; }

        internal SocketReader(Socket socket, string name, CancellationToken ct_disposed, ILogger logger)
        {
            Socket = socket;
            Name = name;
            Ct_disposed = ct_disposed;
            Logger = logger;
            Logger.LogTrace($"{Name}: constructing SocketReader");
            Args.Completed += (_, __) => Semaphore.Release();
            Args.SetBuffer(Buffer, 0, BufferLength);
            ReceiveObservable = CreateReceiveObservable();
        }

        internal async IAsyncEnumerable<byte> ReadBytesAsync()
        {
            while (true)
            {
                Ct_disposed.ThrowIfCancellationRequested();
                if (Position == Args.BytesTransferred)
                {
                    if (Socket.ReceiveAsync(Args))
                        await Semaphore.WaitAsync(Ct_disposed).ConfigureAwait(false);
                    Position = 0;
                    Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} received {Args.BytesTransferred} bytes async from {Socket.RemoteEndPoint}.");
                    if (Args.BytesTransferred == 0)
                        yield break;
                }
                yield return Buffer[Position++];
            }
        }

        private IObservable<byte> CreateReceiveObservable()
        {
            Logger.LogTrace($"{Name}: CreateReceiveObservable.");

            return Observable.Create<byte>((observer, ct_unsubscribed) =>
            {
                Logger.LogTrace($"{Name}: subscribing to ReceiveObservable");

                if (Ct_disposed.IsCancellationRequested)
                {
                    observer.OnError(new ObjectDisposedException("Disposed"));
                    return Task.CompletedTask;
                }

                return Task.Run(() =>
                {
                    var ct = CancellationTokenSource.CreateLinkedTokenSource(Ct_disposed, ct_unsubscribed).Token;

                    try
                    {
                        while (true)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (Position == Args.BytesTransferred)
                            {
                                if (Socket.ReceiveAsync(Args))
                                    Semaphore.Wait(ct);
                                Position = 0;
                                Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} received {Args.BytesTransferred} bytes from {Socket.RemoteEndPoint}.");
                                if (Args.BytesTransferred == 0)
                                {
                                    observer.OnCompleted();
                                    return;
                                }
                            }
                            observer.OnNext(Buffer[Position++]);
                        }
                    }
                    catch (Exception e)
                    {
                        if (ct_unsubscribed.IsCancellationRequested)
                            observer.OnError(new ObjectDisposedException("Disposed"));
                        if (Ct_disposed.IsCancellationRequested)
                            observer.OnError(new ObjectDisposedException("Disposed"));
                        else
                        {
                            Logger.LogDebug($"{Name} on {Socket.LocalEndPoint} ReceiveObservable Exception: {e.Message}\r\n{e}");
                            observer.OnError(e);
                        }
                    }
                });
            });
        }
    }
}
