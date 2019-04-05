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
            var ex = await Disconnector.DisconnectAsync();
            var se = ex as SocketException;
            Assert.Equal(SocketError.Success, se?.SocketErrorCode);
            Assert.True(Disconnector.DisconnectRequested);
        }

        [Fact]
        public async Task T02_DisconnectConnectedSocket()
        {
            Connect();
            var ex = await Disconnector.DisconnectAsync();
            var se = ex as SocketException;
            Assert.Equal(SocketError.Success, se?.SocketErrorCode);

            Assert.True(Disconnector.DisconnectRequested);

            ex = await Disconnector.DisconnectAsync();
            se = ex as SocketException;
            Assert.Equal(SocketError.Success, se?.SocketErrorCode);
        }

        [Fact]
        public async Task T04_Timeout()
        {
            Connect();
            var ex = await Disconnector.DisconnectAsync(0);
            var se = ex as SocketException;
            Assert.Equal(SocketError.TimedOut, se?.SocketErrorCode);
        }

        [Fact]
        public async Task T05_Cancel()
        {
            Connect();
            var token = new CancellationToken(true);
            var ex = await Disconnector.DisconnectAsync(-1, token);
            Assert.IsAssignableFrom<OperationCanceledException>(ex);
        }

        [Fact]
        public async Task T05_DisconnectDisposedSocket()
        {
            Connect();
            Socket.Dispose();
            var se = await Disconnector.DisconnectAsync() as SocketException;
            Assert.Equal(SocketError.Success, se!.SocketErrorCode);
        }
    }
}
