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
            var (_, error) = await SocketConnector.ConnectAsync(EndPoint);
            Assert.Equal(SocketError.ConnectionRefused, error);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var (_, error) = await SocketConnector.ConnectAsync(EndPoint, 0);
            Assert.Equal(SocketError.TimedOut, error);
        }

        [Fact]
        public async Task T04_Cancel()
        {
            var ct = new CancellationToken(true);
            await Assert.ThrowsAsync<OperationCanceledException>(async() =>
                await SocketConnector.ConnectAsync(EndPoint, -1, ct));
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

