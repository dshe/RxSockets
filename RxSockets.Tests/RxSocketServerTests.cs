using System;
using Xunit;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Reactive.Threading.Tasks;
using System.Reactive;

namespace RxSockets.Tests
{
    public class RxSocketServerTest
    {
        private readonly IPEndPoint EndPoint = NetworkHelper.GetEndPointOnLoopbackRandomPort();

        [Fact]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.Throws<SocketException>(() => RxSocketServer.Create(endPoint));
        }

        [Fact]
        public async Task T02_AcceptSuccess()
        {
            var server = RxSocketServer.Create(EndPoint);

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var clientSocket = NetworkHelper.CreateSocket();
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
            await server.AcceptObservable.LastOrDefaultAsync();
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
