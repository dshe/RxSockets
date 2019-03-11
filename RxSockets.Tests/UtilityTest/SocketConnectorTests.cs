using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace RxSockets.Tests
{
    public class SocketConnectorTest
    {
        private readonly IPEndPoint EndPoint = NetworkHelper.GetEndPointOnLoopbackRandomPort();

        [Fact]
        public async Task T01_NoConnection()
        {
            var ex = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(EndPoint));
            Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var ex = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(EndPoint, timeout: 0));
            Assert.Equal(SocketError.TimedOut, ex.SocketErrorCode);
        }

        [Fact]
        public async Task T04_Cancel()
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => 
                await SocketConnector.ConnectAsync(EndPoint , ct:new CancellationToken(true)));
        }

        [Fact]
        public async Task T99_Success()
        {
            var serverSocket = NetworkHelper.CreateSocket();

            serverSocket.Bind(EndPoint);
            serverSocket.Listen(10);

            var socket = await SocketConnector.ConnectAsync(EndPoint);

            socket.Dispose();
            serverSocket.Dispose();
        }

    }

}

