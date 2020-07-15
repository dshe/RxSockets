using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Xunit.Abstractions;
using System.Net.Sockets;

namespace RxSockets.xUnitTests
{
    public class RxSocket_Client_Tests : TestBase
    {
        public RxSocket_Client_Tests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T00_All_Ok()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            await server.AcceptObservable.FirstAsync().ToTask();

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T00_Cancellation_During_Connect()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await endPoint.ConnectRxSocketClientAsync(SocketClientLogger, ct: new CancellationToken(true)));
        }

        [Fact]
        public async Task T00_Timeout_During_Connect()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            await Assert.ThrowsAsync<SocketException>(async () =>
                await endPoint.ConnectRxSocketClientAsync(SocketClientLogger));
        }

        [Fact]
        public async Task T01_Dispose_Before_Receive()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            await client.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await client.ReceiveObservable.ToList());
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T02_Dispose_During_Receive()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var receiveTask = client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await Task.Delay(10);
            await client.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await receiveTask);
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T03_External_Dispose_Before_Receive()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await server.AcceptObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await client.ReceiveObservable.LastOrDefaultAsync();
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T04_External_Dispose_During_Receive()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await server.AcceptObservable.FirstAsync().ToTask();
            var receiveTask = client.ReceiveObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T05_Dispose_Before_Send()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            await client.DisposeAsync();
            Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T06_Dispose_During_Send()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            await client.DisposeAsync();
            await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
            await server.DisposeAsync();
        }

    }
}

