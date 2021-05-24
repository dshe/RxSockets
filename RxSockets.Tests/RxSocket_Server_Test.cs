using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Xunit.Abstractions;

namespace AeSockets.Tests
{
    public class AeSocket_Server_Test : TestBase
    {
        public AeSocket_Server_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void T01_Invalid_EndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.Throws<SocketException>(() => AeSocketServer.Create(endPoint, SocketServerLogger));
        }

        [Fact]
        public async Task T02_Accept_Success()
        {
            var server = AeSocketServer.Create(SocketServerLogger);
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
        public async Task T03_Disconnect_Before_Accept()
        {
            var server = AeSocketServer.Create(SocketServerLogger);
            await server.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException> (async () => await server.AcceptObservable.LastOrDefaultAsync());
        }

        [Fact]
        public async Task T04_Disconnect_While_Accept()
        {
            var server = AeSocketServer.Create(SocketServerLogger);
            var acceptTask = server.AcceptObservable.LastAsync().ToTask();
            await server.DisposeAsync();
            await Assert.ThrowsAsync<ObjectDisposedException>(async () => await acceptTask);
        }
    }
}
