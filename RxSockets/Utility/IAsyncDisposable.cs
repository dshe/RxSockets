using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace RxSockets
{
    public interface IAsyncDisconnectable
    {
        Task DisconnectAsync(CancellationToken ct = default);
    }
}
