﻿using System.Runtime.CompilerServices;

namespace RxSockets;

internal sealed class SocketReceiver
{
    private readonly Memory<byte> Memory = new(new byte[0x1000]);
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly string Name;
    private int Position;
    private int BytesReceived;

    internal SocketReceiver(Socket socket, string name, ILogger logger)
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

                Position = 0;

                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.LogTrace("{Name} on {LocalEndPoint} received {BytesReceived} bytes from {RemoteEndPoint}.", Name, Socket.LocalEndPoint, BytesReceived, Socket.RemoteEndPoint);
            }
            yield return Memory.Span[Position++];
        }
    }
}
