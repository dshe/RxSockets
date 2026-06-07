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
            SocketAcceptor acceptor = new(serverSocket, LogFactory.CreateLogger<SocketAcceptor>(), TestContext.Current.CancellationToken);
            await foreach (IRxSocketClient cli in acceptor.CreateAcceptAllAsync(TestContext.Current.CancellationToken))
            {
                Logger.LogDebug("client");
            }
        }, TestContext.Current.CancellationToken);

        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        Assert.True(client.Connected);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        await client.DisposeAsync();
    }
}
