using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

#nullable enable

namespace RxSockets.Tests
{ 
    public class RxSocketTest : IAsyncLifetime
    {
        private readonly IPEndPoint EndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        private IRxSocketClient? Client, Accept;
        private IRxSocketServer? Server;
        private Task<IRxSocketClient>? AcceptTask;

        public async Task InitializeAsync()
        {
            Server = RxSocketServer.Create(EndPoint);
            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
            (Client, _) = await RxSocketClient.ConnectAsync(EndPoint);
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
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await RxSocketClient.ConnectAsync(EndPoint, ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T01_DisconnectBeforeReceive()
        {
            if (Client == null)
                throw new Exception("Client is null.");
            await Client.DisconnectAsync();
            Assert.Empty(await Client.ReceiveObservable.ToList());
        }

        [Fact]
        public async Task T02_DisconnectDuringReceive()
        {
            if (Client == null)
                throw new Exception("Client is null.");
            var receiveTask = Client.ReceiveObservable.LastOrDefaultAsync().ToTask();
            await Client.DisconnectAsync();
        }

        [Fact]
        public async Task T03_ExternalDisconnectBeforeReceive()
        {
            if (Client == null || Accept == null)
                throw new Exception("Accept or Client is null.");
            await Accept.DisconnectAsync();
            await Client.ReceiveObservable.LastOrDefaultAsync();
        }

        [Fact]
        public async Task T04_ExternalDisconnectDuringReceive()
        {
            if (Client == null || Accept == null)
                throw new Exception("Accept or Client is null.");
            var receiveTask = Client.ReceiveObservable.FirstAsync().ToTask();
            await Accept.DisconnectAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        }

        [Fact]
        public async Task T05_DisconnectBeforeSend()
        {
            if (Client == null)
                throw new Exception("Client is null.");
            IRxSocketClient client = Client;
            await Client.DisconnectAsync();
            Assert.Throws<ObjectDisposedException>(() => client.Send(new byte[] { 0 }));
        }

        [Fact]
        public async Task T06_DisconnectDuringSend()
        {
            if (Client == null)
                throw new Exception("Client is null.");
            IRxSocketClient client = Client;
            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running && sendTask.Status != TaskStatus.RanToCompletion)
                await Task.Yield();
            await Client.DisconnectAsync();
            //await Assert.ThrowsAsync<ObjectDisposedException>(async () => await sendTask);
            //await Assert.ThrowsAsync<SocketException>(async () => await sendTask);
            //await Assert.ThrowsAsync<Exception>(async () => await sendTask);
        }

        [Fact]
        public async Task T07_ExternalDisconnectBeforeSend()
        {
            if (Client == null || Accept == null)
                throw new Exception("Client or Accept is null.");
            await Accept.DisconnectAsync();
            Client.Send(new byte[] { 0,1,2,3 });
            //Assert.Throws<SocketException>(() => Client.Send(new byte[] { 0,1,2,3 }));
        }

        [Fact]
        public async Task T08_ExternalDisconnectDuringSend()
        {
            if (Client == null || Accept == null)
                throw new Exception("Client is null.");
            IRxSocketClient client = Client;

            var sendTask = Task.Run(() => client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running && sendTask.Status != TaskStatus.RanToCompletion)
                await Task.Yield();
            await Accept.DisconnectAsync();
            await sendTask;
            Assert.Throws<SocketException>(() => client.Send(new byte[] { 0 }));
        }
    }
}

