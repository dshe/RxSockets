using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class RxSocketClientTest : TestBase
    {
        public RxSocketClientTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T00_0Ok()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);

            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            await server.AcceptObservable.FirstAsync().ToTask();

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T00_Cancel()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger, ct: new CancellationToken(true)));
        }

        [Fact]
        public async Task T01_DisposeBeforeReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            await client.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async() => await client.ReceiveObservable.ToList());
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T02_DisposeDuringReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var receiveTask = client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await client.DisposeAsync();
            //await Assert.ThrowsAsync<OperationCanceledException>(async () => await receiveTask);
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await receiveTask);
            //Assert.Equal(0, await receiveTask); // default 0
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T03_ExternalDisposeBeforeReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var accept = await server.AcceptObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await client.ReceiveObservable.LastOrDefaultAsync();
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T04_ExternalDisposeDuringReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var accept = await server.AcceptObservable.FirstAsync().ToTask();
            var receiveTask = client.ReceiveObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T05_DisposeBeforeSend()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            await client.DisposeAsync();
            Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T06_DisposeDuringSend()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            await client.DisposeAsync();
            await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
            await server.DisposeAsync();
        }

    }
}

