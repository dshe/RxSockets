using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
namespace RxSockets.Tests;

public class Socket_Acceptor_Tests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task T00_Success()
    {
        EndPoint endPoint = Utilities.CreateIPEndPointOnPort(0);
        Socket serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);
        endPoint = serverSocket.LocalEndPoint ?? throw new InvalidOperationException();

        Task task = Task.Run(async () =>
        {
            SocketAcceptor acceptor = new(serverSocket, LogFactory.CreateLogger<SocketAcceptor>(), default);
            await foreach (IRxSocketClient cli in acceptor.CreateAcceptAllAsync(default))
            {
                Logger.LogDebug("client");
            }
        });

        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(LogFactory, default);
        Assert.True(client.Connected);

        await Task.Delay(100);

        await client.DisposeAsync();
    }
}
