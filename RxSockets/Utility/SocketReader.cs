using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
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
        private readonly Socket Socket;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();
        private readonly string Name;
        private int Position;
        internal IObservable<byte> ReceiveObservable { get; }

        internal SocketReader(Socket socket, string name, ILogger logger)
        {
            Socket = socket;
            Name = name;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
            Args.SetBuffer(Buffer, 0, BufferLength);
            ReceiveObservable = CreateReceiveObservable();
        }

        internal async IAsyncEnumerable<byte> ReadAsync([EnumeratorCancellation] CancellationToken ct)
        {
            while (true)
            {
                if (Position == Args.BytesTransferred)
                {
                    if (Socket.ReceiveAsync(Args))
                        await Semaphore.WaitAsync(ct).ConfigureAwait(false);
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
            return Observable.Create<byte>((observer, ct) =>
            {
                Logger.LogTrace($"{Name}: ReceiveObservable subscribing.");

                return Task.Run(() =>
                {
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
                        Logger.LogTrace(e, $"{Name} on {Socket.LocalEndPoint} SocketReader Exception: {e.Message}");
                        observer.OnError(e);
                    }
                }, ct);
            });
        }
    }
}
