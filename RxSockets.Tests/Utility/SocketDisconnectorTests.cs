using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using System.Threading;

#nullable enable

namespace RxSockets.Tests
{
    public class SocketDisconnectorTest
    {
        private readonly IPEndPoint EndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        private readonly Socket ServerSocket = Utilities.CreateSocket();
        private readonly Socket Socket = Utilities.CreateSocket();
        private readonly SocketDisconnector Disconnector;

        public SocketDisconnectorTest() => Disconnector = new SocketDisconnector(Socket);

        private void Connect()
        {
            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(EndPoint);
            Assert.True(Socket.Connected && !Disconnector.DisconnectRequested);
        }

        [Fact]
        public async Task T01_DisconnectNotConnectedSocket()
        {
            Assert.False(Socket.Connected);
            Assert.False(Disconnector.DisconnectRequested);
            var error = await Disconnector.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);
            Assert.True(Disconnector.DisconnectRequested);
        }

        [Fact]
        public async Task T02_DisconnectConnectedSocket()
        {
            Connect();
            var error = await Disconnector.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);

            Assert.True(Disconnector.DisconnectRequested);

            error = await Disconnector.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);
        }

        [Fact]
        public async Task T04_Timeout()
        {
            Connect();
            var error = await Disconnector.DisconnectAsync(0);
            Assert.Equal(SocketError.TimedOut, error);
        }

        [Fact]
        public async Task T05_Cancel()
        {
            Connect();
            var token = new CancellationToken(true);
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await Disconnector.DisconnectAsync(-1, token));
        }

        [Fact]
        public async Task T05_DisconnectDisposedSocket()
        {
            Connect();
            Socket.Dispose();
            var error = await Disconnector.DisconnectAsync();
            Assert.Equal(SocketError.Success, error);
        }
    }
}
