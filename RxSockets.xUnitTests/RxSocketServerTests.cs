using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class RxSocketServerTest : TestBase
    {
        public RxSocketServerTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.Throws<SocketException>(() => new RxSocketServer(endPoint, logger: SocketServerLogger));
        }

        [Fact]
        public async Task T02_AcceptSuccess()
        {
            var server = new RxSocketServer(logger: SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var clientSocket = Utilities.CreateSocket();
            clientSocket.Connect(endPoint);

            var acceptedSocket = await acceptTask;

            Assert.True(clientSocket.Connected && acceptedSocket.Connected);

            clientSocket.Disconnect(false);
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T03_DisconnectBeforeAccept()
        {
            var server = new RxSocketServer(logger: SocketServerLogger);
            var endPoint = server.IPEndPoint;

            await server.DisposeAsync();
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await server.AcceptObservable.LastOrDefaultAsync());
            //await server.AcceptObservable.LastOrDefaultAsync();
        }

        [Fact]
        public async Task T04_DisconnectWhileAccept()
        {
            var server = new RxSocketServer(logger: SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var acceptTask = server.AcceptObservable.LastAsync().ToTask();
            await server.DisposeAsync();
            await Assert.ThrowsAnyAsync<Exception>(async () => await acceptTask);
        }
    }
}
