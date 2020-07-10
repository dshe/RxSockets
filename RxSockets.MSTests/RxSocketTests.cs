using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Net.Sockets;

namespace RxSockets.MSTests
{
    [TestClass]
    public class RxSocketClientTest : TestBase //, IAsyncLifetime
    {
        [TestMethod]
        public async Task T00_0Ok()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);

            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            await server.AcceptObservable.FirstAsync().ToTask();

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T00_Cancel()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
                await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger, ct: new CancellationToken(true)));
        }

        [TestMethod]
        public async Task T01_DisposeBeforeReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            await client.DisposeAsync();
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async() => await client.ReceiveObservable.ToList());
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T02_DisposeDuringReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var receiveTask = client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await client.DisposeAsync();
            //await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await receiveTask);
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await receiveTask);
            //Assert.AreEqual(0, await receiveTask); // default
            await server.DisposeAsync();
        }

        [TestMethod]
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

        [TestMethod]
        public async Task T04_ExternalDisposeDuringReceive()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var accept = await server.AcceptObservable.FirstAsync().ToTask();
            var receiveTask = client.ReceiveObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await receiveTask);
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T05_DisposeBeforeSend()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            await client.DisposeAsync();
            Assert.ThrowsException<SocketException>(() => client.Send(new byte[] { 0 }));
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T06_DisposeDuringSend()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            await client.DisposeAsync();
            await Assert.ThrowsExceptionAsync<SocketException>(async () => await sendTask);
            await server.DisposeAsync();
        }
    }
}

