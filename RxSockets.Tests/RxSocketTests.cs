using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

namespace RxSockets.Tests
{ 
    public class RxSocketTest : IAsyncLifetime
    {
        private readonly IPEndPoint EndPoint = NetworkHelper.GetEndPointOnLoopbackRandomPort();
        private IRxSocket Client, Accept;
        private IRxSocketServer Server;
        private Task<IRxSocket> AcceptTask;

        public async Task InitializeAsync()
        {
            Server = RxSocketServer.Create(EndPoint);
            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
            Client = (await RxSocket.TryConnectAsync(EndPoint)).rxsocket;
            Accept = await AcceptTask;
        }

        public async Task DisposeAsync()
        {
            if (Client != null) await Client.DisconnectAsync();
            if (Accept != null) await Accept.DisconnectAsync();
            if (Server != null) await Server.DisconnectAsync();
        }

        [Fact]
        public async Task T00_Cancel()
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async() => 
                await RxSocket.TryConnectAsync(EndPoint, ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T01_DisconnectBeforeReceive()
        {
            await Client.DisconnectAsync();
            Assert.Empty(await Client.ReceiveObservable.ToList());
        }

        [Fact]
        public async Task T02_DisconnectDuringReceive()
        {
            var receiveTask = Client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await Client.DisconnectAsync();
        }

        [Fact]
        public async Task T03_ExternalDisconnectBeforeReceive()
        {
            await Accept.DisconnectAsync();
            await Client.ReceiveObservable.LastOrDefaultAsync();
        }

        [Fact]
        public async Task T04_ExternalDisconnectDuringReceive()
        {
            var receiveTask = Client.ReceiveObservable.FirstAsync().ToTask();
            await Accept.DisconnectAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        }

        [Fact]
        public async Task T05_DisconnectBeforeSend()
        {
            await Client.DisconnectAsync();
            Assert.Throws<ObjectDisposedException>(() => Client.Send(new byte[] { 0 }));
        }

        [Fact]
        public async Task T06_DisconnectDuringSend()
        {
            var sendTask = Task.Run(() => Client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running)
                await Task.Yield();
            await Client.DisconnectAsync();
            //await Assert.ThrowsAsync<ObjectDisposedException>(async () => await sendTask);
            //await Assert.ThrowsAsync<SocketException>(async () => await sendTask);
            //await Assert.ThrowsAsync<Exception>(async () => await sendTask);
        }

        [Fact]
        public async Task T07_ExternalDisconnectBeforeSend()
        {
            await Accept.DisconnectAsync();
            Client.Send(new byte[] { 0,1,2,3 });
            //Assert.Throws<SocketException>(() => Client.Send(new byte[] { 0,1,2,3 }));
        }

        [Fact]
        public async Task T08_ExternalDisconnectDuringSend()
        {
            var sendTask = Task.Run(() => Client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running)
                await Task.Yield();
            await Accept.DisconnectAsync();
            await sendTask;
            Assert.Throws<SocketException>(() => Client.Send(new byte[] { 0 }));
        }
    }
}

