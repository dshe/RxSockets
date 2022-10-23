using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
namespace RxSockets;

internal sealed class SocketAcceptor : IAsyncDisposable
{
    private readonly ILogger Logger;
    private readonly Socket Socket;
    private readonly List<IRxSocketClient> AcceptedClients = new(); // state

    internal SocketAcceptor(Socket socket, ILogger logger)
    {
        Socket = socket;
        Logger = logger;
    }

    internal async IAsyncEnumerable<IRxSocketClient> AcceptAllAsync([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket acceptSocket;
            try
            {
                acceptSocket = await Socket.AcceptAsync(ct).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (ct.IsCancellationRequested)
                    yield break;
                Logger.LogError(e, "SocketAcceptor on {LocalEndPoint}. {Message}", Socket.LocalEndPoint, e.Message);
                throw; // ??
            }

            Logger.LogDebug("AcceptClient on {LocalEndPoint} connected to {RemoteEndPoint}.", Socket.LocalEndPoint, acceptSocket.RemoteEndPoint);

            RxSocketClient client = new(acceptSocket, Logger, "AcceptClient");
            AcceptedClients.Add(client);
            yield return client;
        }
    }

    public async ValueTask DisposeAsync()
    {
        List<Task> tasks = AcceptedClients.Select(client => client.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
