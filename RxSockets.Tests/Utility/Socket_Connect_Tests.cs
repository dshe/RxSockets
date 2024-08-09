using System.Threading;
using System.Threading.Tasks;

namespace RxSockets.Tests;

public class SocketConnectTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task T00_Success()
    {
        EndPoint endPoint = Utilities.CreateIPEndPointOnPort(0);
        Socket serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);
        EndPoint actualServerEndpoint = serverSocket.LocalEndPoint ?? throw new InvalidOperationException();

        IRxSocketClient client = await actualServerEndpoint.CreateRxSocketClientAsync(Logger);
        Assert.True(client.Connected);

        await client.DisposeAsync();
        serverSocket.Dispose();
    }

    [Fact]
    public async Task T01_Connection_Refused_Test()
    {
        IPEndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        SocketException e = await Assert.ThrowsAsync<SocketException>(async () => await endPoint.CreateRxSocketClientAsync(Logger));
        Assert.Equal((int)SocketError.ConnectionRefused, e.ErrorCode);
    }

    [Fact]
    public async Task T02_Timeout()
    {
        IPEndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(1));
        CancellationToken ct = cts.Token;
        //await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await Assert.ThrowsAnyAsync<Exception>(async () =>
           await endPoint.CreateRxSocketClientAsync(Logger, ct));
    }

    [Fact]
    public async Task T03_Cancellation()
    {
        IPEndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        CancellationToken ct = new(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
           await endPoint.CreateRxSocketClientAsync(Logger, ct));
    }

}
