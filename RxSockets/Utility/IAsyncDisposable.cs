using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    public interface IAsyncDisconnectable
    {
        Task<Exception> DisconnectAsync(CancellationToken ct = default);
    }
}
