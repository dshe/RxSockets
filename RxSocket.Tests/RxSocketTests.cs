using RxSocket.Tests.Utility;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;
using System.Reactive;

namespace RxSocket.Tests
{ 
    public class XSocketTests : IAsyncLifetime
    {
        private IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());
        private IRxSocket Client, Accept;
        private IRxSocketServer Server;
        private Task<IRxSocket> AcceptTask;

        public async Task InitializeAsync()
        {
            Server = RxSocketServer.Create(EndPoint);
            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
            Client = (await RxSocket.ConnectAsync(EndPoint)).rxsocket;
            Accept = await AcceptTask;
        }

        public async Task DisposeAsync()
        {
            if (Client != null) await Client.DisconnectAsync();
            if (Accept != null) await Accept.DisconnectAsync();
            if (Server != null) await Server.DisconnectAsync();
        }

        [Fact]
        public async Task T00_DisconnectBeforeReceive()
        {
            await Client.DisconnectAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await Client.ReceiveObservable.FirstAsync());
        }

        [Fact]
        public async Task T00_DisconnectDuringReceive()
        {
            var receiveTask = Client.ReceiveObservable.Take(1).ToTask();
            await Client.DisconnectAsync();
            await Assert.ThrowsAsync<SocketException>(async () => await receiveTask);
        }

        [Fact]
        public async Task T00_ExternalDisconnectBeforeReceive()
        {
            await Accept.DisconnectAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Client.ReceiveObservable.FirstAsync());
        }

        [Fact]
        public async Task T00_ExternalDisconnectDuringReceive()
        {
            var receiveTask = Client.ReceiveObservable.FirstAsync().ToTask();
            await Accept.DisconnectAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await receiveTask);
        }

        [Fact]
        public async Task T00_DisconnectBeforeSend()
        {
            await Client.DisconnectAsync();
            Assert.Throws<SocketException>(() => Client.Send(new byte[] { 0 }));
        }

        [Fact]
        public async Task T00_DisconnectDuringSend()
        {
            var sendTask = Task.Run(() => Client.Send(new byte[100_000_000]));
            while (sendTask.Status != TaskStatus.Running)
                await Task.Yield();
            await Client.DisconnectAsync();
            await Assert.ThrowsAsync<SocketException>(async () => await sendTask);
        }

        [Fact]
        public async Task T00_ExternalDisconnectBeforeSend()
        {
            await Accept.DisconnectAsync();
            Client.Send(new byte[] { 0 });
            Assert.Throws<SocketException>(() => Client.Send(new byte[] { 0 }));
        }

        [Fact]
        public async Task T00_ExternalDisconnectDuringSend()
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

