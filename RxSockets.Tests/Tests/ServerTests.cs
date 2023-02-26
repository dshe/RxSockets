using System.Threading.Tasks;

namespace RxSockets.Tests;

public class ServerTest : TestBase
{
    public ServerTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public void T01_Invalid_EndPoint()
    {
        IPEndPoint endPoint = new(IPAddress.Parse("111.111.111.111"), 1111);
        Assert.Throws<SocketException>(() => RxSocketServer.Create(endPoint, SocketServerLogger));
    }

    [Fact]
    public async Task T02_Accept_Success()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync();

        Socket clientSocket = Utilities.CreateSocket();
        await clientSocket.ConnectAsync(endPoint);

        IRxSocketClient acceptedSocket = await acceptTask;

        Assert.True(clientSocket.Connected && acceptedSocket.Connected);

        await clientSocket.DisconnectAsync(false);
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T03_Disconnect_Before_Accept()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        await server.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await server.AcceptAllAsync.FirstAsync());
    }

    [Fact]
    public async Task T04_Disconnect_While_Accept()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync();
        await server.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await acceptTask);
    }
}

