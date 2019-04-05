using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    public interface IAsyncDisconnectable
    {
        Task<SocketError> DisconnectAsync(int timeout = -1, CancellationToken ct = default);
    }
}
