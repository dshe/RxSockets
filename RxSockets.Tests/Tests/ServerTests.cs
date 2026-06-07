namespace RxSockets.Tests;

public class ServerTest(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public void T01_Invalid_EndPoint()
    {
        IPEndPoint endPoint = new(IPAddress.Parse("111.111.111.111"), 1111);
        Assert.Throws<SocketException>(() => RxSocketServer.Create(endPoint, LogFactory));
    }

    [Fact]
    public async Task T02_Accept_Success()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        EndPoint endPoint = server.LocalEndPoint;

        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken);

        Socket clientSocket = Utilities.CreateSocket();
        await clientSocket.ConnectAsync(endPoint, TestContext.Current.CancellationToken);

        IRxSocketClient acceptedSocket = await acceptTask;

        Assert.True(clientSocket.Connected && acceptedSocket.Connected);

        await clientSocket.DisconnectAsync(false, TestContext.Current.CancellationToken);
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T03_Disconnect_Before_Accept()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        await server.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task T04_Disconnect_While_Accept()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken);
        await server.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await acceptTask);
    }
}

