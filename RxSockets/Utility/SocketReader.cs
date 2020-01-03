using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
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
        private int Position;

        internal SocketReader(Socket socket, CancellationToken ct, ILogger logger)
        {
            Socket = socket;
            Ct = ct;
            Logger = logger;
            Args.Completed += (_, __) => Semaphore.Release();
            Args.SetBuffer(Buffer, 0, BufferLength);
        }

        internal IEnumerable<byte> Read()
        {
            while (true)
            {
                if (Position == Args.BytesTransferred)
                {
                    if (Socket.ReceiveAsync(Args))
                        Semaphore.Wait(Ct);
                    if (Args.BytesTransferred == 0)
                        yield break;
                    Logger.LogTrace($"Received {Args.BytesTransferred} bytes.");
                    Position = 0;
                }
                yield return Buffer[Position++];
            }
        }

        internal async Task<byte> ReadAsync()
        {
            if (Position == Args.BytesTransferred)
            {
                if (Socket.ReceiveAsync(Args))
                    await Semaphore.WaitAsync(Ct).ConfigureAwait(false);
                if (Args.BytesTransferred == 0)
                    throw new SocketException((int)SocketError.NoData);
                Logger.LogTrace($"Received {Args.BytesTransferred} bytes.");
                Position = 0;
            }
            return Buffer[Position++];
        }

        /* Requires NetStardard 2.0 => 2.1
        internal async IAsyncEnumerable<byte> ReadAsync()
        {
            while (!Ct.IsCancellationRequested)
            {
                if (Position < Args.BytesTransferred)
                {
                    yield return Buffer[Position++];
                    continue;
                }

                if (Socket.ReceiveAsync(Args))
                {
                    try
                    {
                        await Semaphore.WaitAsync(Ct);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
                if (Args.BytesTransferred == 0)
                    break;
                Logger.LogTrace($"Received {Args.BytesTransferred} bytes.");
                Position = 0;
            }
            yield break;
        }
        */
    }
}
