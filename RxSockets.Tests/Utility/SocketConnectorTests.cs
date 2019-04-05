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
            var (_, exception) = await SocketConnector.ConnectAsync(EndPoint);
            var se = Assert.IsType<SocketException>(exception);
            Assert.Equal(SocketError.ConnectionRefused, se.SocketErrorCode);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var (_, exception) = await SocketConnector.ConnectAsync(EndPoint, 0);
            var se = Assert.IsType<SocketException>(exception);
            Assert.Equal(SocketError.TimedOut, se.SocketErrorCode);
        }

        [Fact]
        public async Task T04_Cancel()
        {
            var ct = new CancellationToken(true);
            var (_, exception) = await SocketConnector.ConnectAsync(EndPoint, -1, ct);
            Assert.IsType<OperationCanceledException>(exception);
        }

        [Fact]
        public async Task T99_Success()
        {
            var serverSocket = Utilities.CreateSocket();

            serverSocket.Bind(EndPoint);
            serverSocket.Listen(10);

            var (socket, _) = await SocketConnector.ConnectAsync(EndPoint);

            var sd = new SocketDisconnector(socket!);
            await sd.DisconnectAsync();
            serverSocket.Dispose();
        }

    }

}

