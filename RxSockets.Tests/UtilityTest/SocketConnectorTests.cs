using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RxSockets.Tests
{
    public class SocketConnectorTest
    {
        private readonly IPEndPoint EndPoint = NetworkHelper.GetEndPointOnLoopbackRandomPort();

        [Fact]
        public async Task T00_Null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await SocketConnector.TryConnectAsync(null));
        }

        [Fact]
        public async Task T01_NoConnection()
        {
            var (error, xsocket) = await SocketConnector.TryConnectAsync(EndPoint);
            Assert.Equal(SocketError.ConnectionRefused, error);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var (error, socket) = await SocketConnector.TryConnectAsync(EndPoint, timeout: 0);
            Assert.Equal(SocketError.TimedOut, error);
        }

        [Fact]
        public async Task T04_Cancel()
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => 
                await SocketConnector.TryConnectAsync(EndPoint , ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T99_Success()
        {
            var serverSocket = NetworkHelper.CreateSocket();

            serverSocket.Bind(EndPoint);
            serverSocket.Listen(10);

            var (error, socket) = await SocketConnector.TryConnectAsync(EndPoint);
            Assert.Equal(SocketError.Success, error);

            socket.Dispose();
            serverSocket.Dispose();
        }

    }

}

