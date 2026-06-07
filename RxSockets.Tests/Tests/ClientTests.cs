namespace RxSockets.Tests;

public class ClientTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task T00_All_Ok()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(Logger, TestContext.Current.CancellationToken);
        await server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken);
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T00_Cancellation_During_Connect()
    {
        IPEndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await endPoint.CreateRxSocketClientAsync(LogFactory, ct: new CancellationToken(true)));
    }

    [Fact]
    public async Task T00_Timeout_During_Connect()
    {
        IPEndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        await Assert.ThrowsAsync<SocketException>(async () =>
            await endPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task T01_Dispose_Before_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        await client.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ReceiveAllAsync.FirstAsync(TestContext.Current.CancellationToken));
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T02_Dispose_During_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);

        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        ValueTask<byte> receiveTask = client.ReceiveAllAsync.LastOrDefaultAsync(TestContext.Current.CancellationToken);
        await client.DisposeAsync();

        await receiveTask; // does not throw

        await server.DisposeAsync();
    }

    [Fact]
    public async Task T03_External_Dispose_Before_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        IRxSocketClient accept = await server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken);
        await accept.DisposeAsync();
        await client.ReceiveAllAsync.LastOrDefaultAsync(TestContext.Current.CancellationToken);
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T04_External_Dispose_During_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        IRxSocketClient accept = await server.AcceptAllAsync.FirstAsync(TestContext.Current.CancellationToken);
        ValueTask<byte> receiveTask = client.ReceiveAllAsync.FirstAsync(TestContext.Current.CancellationToken);
        await accept.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T05_Dispose_Before_Send()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        await client.DisposeAsync();
        Assert.ThrowsAny<Exception>(() => client.Send([0]));
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T06_Dispose_During_Send()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);

        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory, TestContext.Current.CancellationToken);
        Task<int> sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
        await client.DisposeAsync();
        await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
        await server.DisposeAsync();
    }
}

