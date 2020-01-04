using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class RxSocketClientTest : TestBase, IAsyncLifetime
    {
        public IRxSocketServer Server;
        public RxSocketClientTest(ITestOutputHelper output) : base(output) 
        {
            Server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
        }
        async Task IAsyncLifetime.InitializeAsync()
        {
           await Task.Delay(0);
        }
        async Task IAsyncLifetime.DisposeAsync()
        {
            await Server.DisposeAsync();
        }

        [Fact]
        public async Task T00_0Ok()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            await Server.AcceptObservable.FirstAsync().ToTask();
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T00_Cancel()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger, ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T01_DisposeBeforeReceive()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            await client.DisposeAsync();
            //Assert.Empty(await client.ReceiveObservable.ToList());
            await Assert.ThrowsAsync<OperationCanceledException>(async() => await client.ReceiveObservable.ToList());
        }

        [Fact]
        public async Task T02_DisposeDuringReceive()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var receiveTask = client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await client.DisposeAsync();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await receiveTask);
        }

        [Fact]
        public async Task T03_ExternalDisposeBeforeReceive()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await Server.AcceptObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await client.ReceiveObservable.LastOrDefaultAsync();
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T04_ExternalDisposeDuringReceive()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await Server.AcceptObservable.FirstAsync().ToTask();
            var receiveTask = client.ReceiveObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T05_DisposeBeforeSend()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            await client.DisposeAsync();
            Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
        }

        /*
        [Fact]
        public async Task T06_DisposeDuringSend()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            client.Dispose();
            await Task.Delay(100);
            await Assert.ThrowsAnyAsync<Exception>(async () => await sendTask);
            //await Assert.ThrowsAsync<ObjectDisposedException>(async () => await sendTask);
            //await Assert.ThrowsAsync<Exception>(async () => await sendTask);
        }
        */

        /*
        [Fact]
        public async Task T07_ExternalDisposeBeforeSend()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await Server.AcceptObservable.FirstAsync().ToTask();
            await accept.DisposeAsync();
            await Task.Delay(100);
            //client.Send(new byte[] { 0,1,2,3 });
            Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0,1,2,3 }));
            await client.DisposeAsync();
        }
        */

        /*
        [Fact]
        public async Task T08_ExternalDisposeDuringSend()
        {
            const int bytes = 1; //100_000_000;
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await Server.AcceptObservable.FirstAsync();
            var sendTask = Task.Run(() => client.Send(new byte[bytes]));
            while (sendTask.Status != TaskStatus.Running && sendTask.Status != TaskStatus.RanToCompletion)
                await Task.Yield();
            await accept.DisposeAsync();
            await sendTask;
            Assert.ThrowsAny<Exception>(() => client.Send(new byte[] { 0 }));
            await client.DisposeAsync();
        }
        */
    }
}

