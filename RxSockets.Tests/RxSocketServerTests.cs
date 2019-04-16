using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;

#nullable enable

namespace RxSockets.Tests
{
    public class RxSocketServerTest : TestBase
    {
        public RxSocketServerTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.Throws<SocketException>(() => RxSocketServer.Create(endPoint, SocketServerLogger));
        }

        [Fact]
        public async Task T02_AcceptSuccess()
        {
            var server = RxSocketServer.Create(IPEndPoint, SocketServerLogger);

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var clientSocket = Utilities.CreateSocket();
            clientSocket.Connect(IPEndPoint);

            var acceptedSocket = await acceptTask;

            Assert.True(clientSocket.Connected && acceptedSocket.Connected);

            acceptedSocket.Dispose();
            clientSocket.Disconnect(false);
            server.Dispose();
        }

        [Fact]
        public async Task T03_DisconnectBeforeAccept()
        {
            var server = RxSocketServer.Create(IPEndPoint, SocketServerLogger);
            server.Dispose();
            //Assert.Equal(SocketError.Success, error);
            await Task.Delay(100);
            var result = await server.AcceptObservable.LastOrDefaultAsync().ToTask();
            Assert.Null(result);
        }

        [Fact]
        public async Task T04_DisconnectWhileAccept()
        {
            var server = RxSocketServer.Create(IPEndPoint, SocketServerLogger);
            var acceptTask = server.AcceptObservable.LastOrDefaultAsync().ToTask();
            await Task.Delay(100);
            server.Dispose();
            //Assert.Equal(SocketError.Success, error);
            var result = await acceptTask;
            Assert.Null(result);
        }

    }
}
