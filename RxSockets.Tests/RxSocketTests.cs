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

#nullable enable

namespace RxSockets.Tests
{
    public class RxSocketClientTest : TestBase, IDisposable
    {
        private readonly IRxSocketServer Server;
        private readonly Task<IRxSocketClient> AcceptTask;

        public RxSocketClientTest(ITestOutputHelper output) : base(output)
        {
            Server = RxSocketServer.Create(IPEndPoint, SocketServerLogger);
            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
        }

        public void Dispose()
        {
            Server.Dispose();
        }

        [Fact]
        public async Task T00_0Ok()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var accept = await AcceptTask;
            client.Dispose();
        }

        [Fact]
        public async Task T00_Cancel()
        {
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger, ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T01_DisposeBeforeReceive()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            client.Dispose();
            Assert.Empty(await client.ReceiveObservable.ToList());
        }

        [Fact]
        public async Task T02_DisposeDuringReceive()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var receiveTask = client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            client.Dispose();
            await receiveTask;
        }

        [Fact]
        public async Task T03_ExternalDisposeBeforeReceive()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var accept = await AcceptTask;
            accept.Dispose();
            await client.ReceiveObservable.LastOrDefaultAsync();
            client.Dispose();
        }

        [Fact]
        public async Task T04_ExternalDisposeDuringReceive()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var accept = await AcceptTask;
            var receiveTask = client.ReceiveObservable.FirstAsync().ToTask();
            accept.Dispose();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
            client.Dispose();
        }

        [Fact]
        public async Task T05_DisposeBeforeSend()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            client.Dispose();
            Assert.Throws<ObjectDisposedException>(() => client.Send(new byte[] { 0 }));
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

        [Fact]
        public async Task T07_ExternalDisposeBeforeSend()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var accept = await AcceptTask;
            accept.Dispose();
            client.Send(new byte[] { 0,1,2,3 });
            Assert.Throws<SocketException>(() => client.Send(new byte[] { 0,1,2,3 }));
            client.Dispose();
        }

        [Fact]
        public async Task T08_ExternalDisposeDuringSend()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint, SocketClientLogger);
            var accept = await AcceptTask;
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running && sendTask.Status != TaskStatus.RanToCompletion)
                await Task.Yield();
            accept.Dispose();
            await sendTask;
            Assert.Throws<SocketException>(() => client.Send(new byte[] { 0 }));
            client.Dispose();
        }
    }
}

