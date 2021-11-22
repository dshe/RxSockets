using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests;

public class SocketConnectTests : TestBase
{
    public SocketConnectTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T00_Success()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        var serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        var client = await endPoint.CreateRxSocketClientAsync(Logger);
        Assert.True(client.Connected);

        await client.DisposeAsync();
        serverSocket.Dispose();
    }

    [Fact]
    public async Task T01_Connection_Refused_Test()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        var e = await Assert.ThrowsAsync<SocketException>(async () => await endPoint.CreateRxSocketClientAsync(Logger));
        Assert.Equal((int)SocketError.ConnectionRefused, e.ErrorCode);
    }

    [Fact]
    public async Task T02_Timeout()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));
        var ct = cts.Token;
        //await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        await Assert.ThrowsAnyAsync<Exception>(async () =>
           await endPoint.CreateRxSocketClientAsync(Logger, ct));
    }

    [Fact]
    public async Task T03_Cancellation()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        var ct = new CancellationToken(true);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
           await endPoint.CreateRxSocketClientAsync(Logger, ct));
    }

}
