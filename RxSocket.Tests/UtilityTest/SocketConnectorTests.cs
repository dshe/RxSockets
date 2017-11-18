using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RxSocket.Tests.Utility;

namespace RxSocket.Tests
{
    public class SocketConnectorTests
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
        public async Task T03_Timeout()
        {
            var (error, socket) = await SocketConnector.ConnectAsync(EndPoint, 0);
            Assert.Equal(SocketError.TimedOut, error);
        }

        [Fact]
        public async Task T04_Cancel()
        {
            var cts = new CancellationTokenSource();
            cts.Cancel(true);

            var (error, socket) = await SocketConnector.ConnectAsync(EndPoint , -1, cts.Token);
            Assert.Equal(SocketError.OperationAborted, error);
        }
    }

}

