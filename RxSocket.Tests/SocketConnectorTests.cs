using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RxSocket.Tests.Utility;

namespace RxSocket.Tests
{
    public class SocketConnectorTest
    {
        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());

        [Fact]
        public async Task T00_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await SocketConnector.ConnectAsync(null));
        }

        [Fact]
        public async Task T01_NoConnection()
        {
            var (error, xsocket) = await SocketConnector.ConnectAsync(EndPoint);
            Assert.Equal(SocketError.ConnectionRefused, error);
        }

        [Fact]
        public async Task T02_Connection()
        {
            var serverSocket = NetworkUtility.CreateSocket();

            serverSocket.Bind(EndPoint);
            serverSocket.Listen(10);

            var (error, socket) = await SocketConnector.ConnectAsync(EndPoint);
            Assert.Equal(SocketError.Success, error);
        }

        [Fact]
        public async Task T03_Cancel()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () => 
                await SocketConnector.ConnectAsync(EndPoint , new CancellationToken(true)));
        }
    }

}

