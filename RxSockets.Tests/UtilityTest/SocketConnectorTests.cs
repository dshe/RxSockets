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
        private readonly IPEndPoint EndPoint = Utilities.GetEndPointOnLoopbackRandomPort();

        [Fact]
        public async Task T01_NoConnection()
        {
            var ex = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(EndPoint));
            Assert.Equal(SocketError.ConnectionRefused, ex.SocketErrorCode);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var cts = new CancellationTokenSource(0);
            var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await SocketConnector.ConnectAsync(EndPoint, cts.Token));
        }

        [Fact]
        public async Task T04_Cancel()
        {
            var ct = new CancellationToken(true);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => 
                await SocketConnector.ConnectAsync(EndPoint , ct));
        }

        [Fact]
        public async Task T99_Success()
        {
            var serverSocket = Utilities.CreateSocket();

            serverSocket.Bind(EndPoint);
            serverSocket.Listen(10);

            var socket = await SocketConnector.ConnectAsync(EndPoint);

            var sd = new SocketDisconnector(socket);
            await sd.DisconnectAsync();
            serverSocket.Dispose();
        }

    }

}

