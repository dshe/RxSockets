using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    internal class SocketReader
    {
        public const int BufferLength = 0x1000;
        private readonly byte[] Buffer = new byte[BufferLength];
        private readonly ILogger Logger;
        private readonly CancellationToken Ct;
        private readonly Socket Socket;
        private readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0, 1);
        private readonly SocketAsyncEventArgs Args = new SocketAsyncEventArgs();
        private readonly string Name;
        private readonly IScheduler Scheduler = new NewThreadScheduler(
            ts => new Thread(ts) { IsBackground = false, Name = "SocketReader" });
        private int Position;

        internal SocketReader(Socket socket, string name, CancellationToken ct, ILogger logger)
        {
            Socket = socket;
            Name = name;
            Ct = ct;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
            Args.SetBuffer(Buffer, 0, BufferLength);
        }

        internal async Task<byte> ReadByteAsync()
        {
            Ct.ThrowIfCancellationRequested();
            if (Position == Args.BytesTransferred)
            {
                if (Socket.ReceiveAsync(Args))
                    await Semaphore.WaitAsync(Ct).ConfigureAwait(false);
                if (Args.BytesTransferred == 0)
                    throw new SocketException((int)SocketError.NoData);
                Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} received {Args.BytesTransferred} bytes async from {Socket.RemoteEndPoint}.");
                Position = 0;
            }
            return Buffer[Position++];
        }

        internal IObservable<byte> CreateReceiveObservable()
        {
            return Observable.Create<byte>(observer =>
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(Ct);
                var ct = cts.Token;
                Scheduler.Schedule(() =>
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
                                Logger.LogTrace($"{Name} on {Socket.LocalEndPoint} received {Args.BytesTransferred} bytes from {Socket.RemoteEndPoint}.");
                                if (Args.BytesTransferred == 0)
                                    break;
                                Position = 0;
                            }
                            observer.OnNext(Buffer[Position++]);
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            if (!ct.IsCancellationRequested)
                                Logger.LogDebug($"{Name} on {Socket.LocalEndPoint} Observer Exception: {e.Message}\r\n{e}");
                            observer.OnError(e);
                        }
                        catch (Exception e2)
                        {
                            if (!ct.IsCancellationRequested)
                                Logger.LogDebug($"{Name} on {Socket.LocalEndPoint} OnError Exception: {e2.Message}\r\n{e2}");
                        }
                    }
                });
                return Disposable.Create(() => cts.Cancel());
            });
        }
    }
}
