using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RxSockets
{
    internal class SocketReceiver
    {
        public const int BufferLength = 0x1000;
        private readonly Memory<byte> Memory = new(new byte[BufferLength]);
        private readonly ILogger Logger;
        private readonly Socket Socket;
        private readonly string Name;
        private int Position = 0;
        private int BytesReceived = 0;

        internal SocketReceiver(Socket socket, ILogger logger, string name)
        {
            Socket = socket;
            Name = name;
            Logger = logger;
        }

        internal async IAsyncEnumerable<byte> ReceiveAllAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                if (Position == BytesReceived)
                {
                    try
                    {
                        BytesReceived = await Socket.ReceiveAsync(Memory, SocketFlags.None, ct).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        if (ct.IsCancellationRequested)
                            yield break;
                        throw;
                    }
                    if (BytesReceived == 0)
                        yield break;
                    Logger.LogTrace("{Name} on {LocalEndPoint} received {BytesReceived} bytes from {RemoteEndPoint}.", Name, Socket.LocalEndPoint, BytesReceived, Socket.RemoteEndPoint);
                    Position = 0;
                }
                yield return Memory.Span[Position++];
            }
        }
    }
}
