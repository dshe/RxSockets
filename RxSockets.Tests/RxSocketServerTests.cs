using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class RxSocketServerTest : TestBase
    {
        public RxSocketServerTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.Throws<SocketException>(() => endPoint.CreateRxSocketServer(SocketServerLogger));
        }

        [Fact]
        public async Task T02_AcceptSuccess()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

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
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
            server.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await server.AcceptObservable.LastOrDefaultAsync());
        }

        [Fact]
        public async Task T04_DisconnectWhileAccept()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
            var acceptTask = server.AcceptObservable.LastAsync().ToTask();
            server.Dispose();
            await Assert.ThrowsAnyAsync<Exception>(async () => await acceptTask);
        }

    }
}
