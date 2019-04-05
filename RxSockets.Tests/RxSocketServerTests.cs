using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

#nullable enable

namespace RxSockets.Tests
{
    public class RxSocketServerTest
    {
        private readonly IPEndPoint EndPoint = Utilities.GetEndPointOnLoopbackRandomPort();

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

            var clientSocket = Utilities.CreateSocket();
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
            var error = await server.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);
            await Task.Delay(100);
            var result = await server.AcceptObservable.LastOrDefaultAsync().ToTask();
            Assert.Null(result);
        }

        [Fact]
        public async Task T04_DisconnectWhileAccept()
        {
            var server = RxSocketServer.Create(EndPoint);
            var acceptTask = server.AcceptObservable.LastOrDefaultAsync().ToTask();
            await Task.Delay(100);
            var error = await server.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);
            var result = await acceptTask;
            Assert.Null(result);
        }

    }
}
