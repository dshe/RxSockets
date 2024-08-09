using System.Threading;
using System.Threading.Tasks;

namespace RxSockets.Tests;

public class ClientTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task T00_All_Ok()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);

        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(Logger);

        //await server.AcceptAllAsync().ToObservableFromAsyncEnumerable().FirstAsync();
        await server.AcceptAllAsync.FirstAsync();

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
            await endPoint.CreateRxSocketClientAsync(LogFactory));
    }

    [Fact]
    public async Task T01_Dispose_Before_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        await client.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.ReceiveAllAsync.FirstAsync());
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T02_Dispose_During_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);

        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        ValueTask<byte> receiveTask = client.ReceiveAllAsync.LastOrDefaultAsync();
        await client.DisposeAsync();

        //await Assert.ThrowsAsync<SocketException>(async () =>
        //    await receiveTask);
        await receiveTask;

        await server.DisposeAsync();
    }

    [Fact]
    public async Task T03_External_Dispose_Before_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        IRxSocketClient accept = await server.AcceptAllAsync.FirstAsync();
        await accept.DisposeAsync();
        await client.ReceiveAllAsync.LastOrDefaultAsync();
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T04_External_Dispose_During_Receive()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        IRxSocketClient accept = await server.AcceptAllAsync.FirstAsync();
        ValueTask<byte> receiveTask = client.ReceiveAllAsync.FirstAsync();
        await accept.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T05_Dispose_Before_Send()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        await client.DisposeAsync();
        Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T06_Dispose_During_Send()
    {
        IRxSocketServer server = RxSocketServer.Create(LogFactory);

        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(LogFactory);
        Task<int> sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
        await client.DisposeAsync();
        await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
        await server.DisposeAsync();
    }
}

