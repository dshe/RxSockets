using System;
using Xunit;
using RxSocket.Tests.Utility;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Reactive.Threading.Tasks;
using System.Reactive;

namespace RxSocket.Tests
{
    public class RxSocketServerTest
    {
        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());

        [Fact]
        public void T00_Null()
        {
            Assert.Throws<ArgumentNullException>(() => RxSocketServer.Create(null));
        }

        [Fact]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("192.168.254.254"), 80);
            Assert.Throws<SocketException>(() => RxSocketServer.Create(endPoint));
        }

        [Fact]
        public async Task T02_AcceptSuccess()
        {
            var server = RxSocketServer.Create(EndPoint);

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var clientSocket = NetworkUtility.CreateSocket();
            clientSocket.Connect(EndPoint);

            var acceptedSocket = await acceptTask;

            Assert.True(clientSocket.Connected && acceptedSocket.Connected);

            await acceptedSocket.DisconnectAsync();
            clientSocket.Disconnect(false);
            await server.DisconnectAsync();
        }

        [Fact]
        public async Task T03_DisconnectBeforeAccept()
        {
            var server = RxSocketServer.Create(EndPoint);
            await server.DisconnectAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await server.AcceptObservable.LastOrDefaultAsync());
        }

        [Fact]
        public async Task T04_DisconnectWhileAccept()
        {
            var server = RxSocketServer.Create(EndPoint);
            var acceptTask = server.AcceptObservable.LastOrDefaultAsync().ToTask();
            await Task.Yield();
            await server.DisconnectAsync();
            await acceptTask;
        }

    }
}
