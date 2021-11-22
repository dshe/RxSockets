using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Xunit.Abstractions;
using System.Net.Sockets;

namespace RxSockets.Tests;

public class ClientTests : TestBase
{
    public ClientTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T00_All_Ok()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var client = await server.IPEndPoint.CreateRxSocketClientAsync(Logger);

        //await server.AcceptObservable.FirstAsync();
        //await server.AcceptAllAsync().ToObservableFromAsyncEnumerable().FirstAsync();
        await server.AcceptAllAsync().FirstAsync();

        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T00_Cancellation_During_Connect()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await endPoint.CreateRxSocketClientAsync(SocketClientLogger, ct: new CancellationToken(true)));
    }

    [Fact]
    public async Task T00_Timeout_During_Connect()
    {
        var endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        await Assert.ThrowsAsync<SocketException>(async () =>
            await endPoint.CreateRxSocketClientAsync(SocketClientLogger));
    }

    [Fact]
    public async Task T01_Dispose_Before_Receive()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var client = await server.IPEndPoint.CreateRxSocketClientAsync(SocketClientLogger);
        await client.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.ReceiveAllAsync().ToListAsync());
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T02_Dispose_During_Receive()
    {
        var server = RxSocketServer.Create(SocketServerLogger);

        var client = await server.IPEndPoint.CreateRxSocketClientAsync(SocketClientLogger);
        var receiveTask = client.ReceiveAllAsync().LastOrDefaultAsync();
        //await Task.Delay(100);
        await client.DisposeAsync();

        //await Assert.ThrowsAsync<SocketException>(async () =>
        //    await receiveTask);
        await receiveTask;

        await server.DisposeAsync();
    }

    [Fact]
    public async Task T03_External_Dispose_Before_Receive()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var client = await server.IPEndPoint.CreateRxSocketClientAsync(SocketClientLogger);
        var accept = await server.AcceptAllAsync().FirstAsync();
        await accept.DisposeAsync();
        await client.ReceiveAllAsync().LastOrDefaultAsync();
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T04_External_Dispose_During_Receive()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var client = await server.IPEndPoint.CreateRxSocketClientAsync(SocketClientLogger);
        var accept = await server.AcceptAllAsync().FirstAsync();
        var receiveTask = client.ReceiveAllAsync().FirstAsync();
        await accept.DisposeAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T05_Dispose_Before_Send()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var endPoint = server.IPEndPoint;
        var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        await client.DisposeAsync();
        Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T06_Dispose_During_Send()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var endPoint = server.IPEndPoint;

        var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
        await client.DisposeAsync();
        await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
        await server.DisposeAsync();
    }
}

