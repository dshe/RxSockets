using System.Net.Sockets;
using Xunit;
using Xunit.Abstractions;



namespace RxSockets.Tests
{
    public class SocketDisposerTest: TestBase
    {
        private readonly Socket ServerSocket = Utilities.CreateSocket();
        private readonly Socket Socket = Utilities.CreateSocket();
        private readonly SocketDisposer Disposer;

        public SocketDisposerTest(ITestOutputHelper output) : base(output)
        {
            Disposer = new SocketDisposer(Socket, Logger);
        }

        private void Connect()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);
            Assert.True(Socket.Connected);
            Assert.True(!Disposer.DisposeRequested);
        }

        [Fact]
        public void T01_DisposeNotConnectedSocket()
        {
            Assert.False(Socket.Connected);
            Assert.False(Disposer.DisposeRequested);
            Disposer.DisposeAsync();
            Assert.True(Disposer.DisposeRequested);
        }

        [Fact]
        public void T02_DisposeConnectedSocket()
        {
            Connect();
            Disposer.DisposeAsync();
            Assert.True(Disposer.DisposeRequested);
            Disposer.DisposeAsync();
        }


        [Fact]
        public void T05_DisposeDisposedSocket()
        {
            Connect();
            Socket.Dispose();
            Disposer.DisposeAsync();
        }
    }
}
