using System;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using RxSocket.Tests.Utility;

namespace RxSocket.Tests
{
    public class SocketAcceptorTests : IDisposable
    {
        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());
        private readonly Socket Socket = NetworkUtility.CreateSocket();
        private readonly Socket ServerSocket = NetworkUtility.CreateSocket();
        private readonly SocketAcceptor Acceptor;

        public SocketAcceptorTests() 
        {
            ServerSocket.Bind(EndPoint);
            Acceptor = new SocketAcceptor(ServerSocket);
        }

        public void Dispose()
        {
            Socket.Dispose();
            ServerSocket.Dispose();
        }

        [Fact]
        public void T00_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SocketAcceptor(null));
        }

        [Fact]
        public async Task T01_Success()
        {
            var task = Task.Run(() => Acceptor.Accept());
            Socket.Connect(EndPoint);
            var (error, acceptedSocket) = await task;
            Assert.Equal(SocketError.Success, error);
            Assert.True(Socket.Connected && acceptedSocket.Connected);
        }

        [Fact]
        public void T05_DisposeBeforeAccept()
        {
            ServerSocket.Dispose();

            var (error, socket) = Acceptor.Accept();

            Assert.Equal(SocketError.Shutdown, error);
            Assert.Null(socket);
        }

        [Fact]
        public async Task T06_DisposeDuringAccept()
        {

            var task = Task.Run(() => Acceptor.Accept());

            await Task.Delay(10);

            ServerSocket.Dispose();

            var(error, socket) = await task;
            Assert.Equal(SocketError.OperationAborted, error);
            Assert.Null(socket);
        }

    }
}

