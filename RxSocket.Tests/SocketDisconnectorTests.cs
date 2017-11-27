using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using RxSocket.Tests.Utility;

namespace RxSocket.Tests
{
    public class SocketDisconnectorTest
    {
        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());
        private readonly Socket ServerSocket = NetworkUtility.CreateSocket();
        private readonly Socket Socket = NetworkUtility.CreateSocket();
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
        public void T00_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SocketDisconnector(null));
        }

        [Fact]
        public async Task T01_DisconnectNotConnectedSocket()
        {
            Assert.False(Socket.Connected);
            Assert.False(Disconnector.DisconnectRequested);
            Assert.Equal(SocketError.NotConnected, await Disconnector.DisconnectAsync());
            Assert.True(Disconnector.DisconnectRequested);
        }

        [Fact]
        public async Task T02_DisconnectConnectedSocket()
        {
            Connect();

            Assert.Equal(SocketError.Success, await Disconnector.DisconnectAsync());
            Assert.True(Disconnector.DisconnectRequested);
            Assert.Equal(SocketError.Success, await Disconnector.DisconnectAsync());
        }

        /*
        [Fact]
        public async Task T03_Cancel()
        {
            Connect();
            Assert.Equal(SocketError.OperationAborted, await Disconnector.DisconnectAsync(new CancellationTokenSource(0).Token));
        }
        */

        [Fact]
        public async Task T04_DisconnectDisposedSocket()
        {
            Socket.Dispose();
            Assert.Equal(SocketError.Shutdown, await Disconnector.DisconnectAsync());
            Assert.True(Disconnector.DisconnectRequested);
        }

        /*
        [Fact]
        public async Task T05_DisconnectRequested()
        {
            Connect();

            Disconnector.DisconnectAsync(); // fire and forget
            Assert.True(Disconnector.DisconnectRequested);

            Assert.Equal(SocketError.Success, await Disconnector.DisconnectAsync());
        }
        */
    }
}
