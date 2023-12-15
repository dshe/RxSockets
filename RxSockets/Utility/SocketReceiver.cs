using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace RxSockets;

internal sealed class SocketReceiver
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly string Name;
    private int Position;
    private int BytesReceived;
#if NETSTANDARD2_0
    private readonly byte[] Buffer = new byte[0x1000];
    private readonly NetworkStream Stream; 
#else
    private readonly Memory<byte> Memory = new(new byte[0x1000]);
#endif

    internal SocketReceiver(Socket socket, string name, ILogger logger)
    {
        Socket = socket;
        
#if NETSTANDARD2_0
        Stream = new NetworkStream(Socket, false);
#endif

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
#if NETSTANDARD2_0
                    //BytesReceived = await Task.Run(() => Socket.Receive(Buffer, Position, Buffer.Length - Position, SocketFlags.None), ct).ConfigureAwait(false);
                    BytesReceived = await Stream.ReadAsync(Buffer, 0, Buffer.Length, ct);
#else
                    BytesReceived = await Socket.ReceiveAsync(Memory, SocketFlags.None, ct).ConfigureAwait(false);
#endif
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
#if NETSTANDARD2_0
            yield return Buffer[Position++];
#else
            yield return Memory.Span[Position++];
#endif
        }
    }
}
