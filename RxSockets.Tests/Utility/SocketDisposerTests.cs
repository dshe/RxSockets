using System;
using System.Net.Sockets;
using System.Threading.Tasks;
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
        public async Task T01_DisposeNotConnectedSocket()
        {
            Assert.False(Socket.Connected);
            Assert.False(Disposer.DisposeRequested);
            await Disposer.DisposeAsync();
            Assert.True(Disposer.DisposeRequested);
        }

        [Fact]
        public async Task T02_DisposeConnectedSocket()
        {
            Connect();
            await Disposer.DisposeAsync();
            Assert.True(Disposer.DisposeRequested);
            await Disposer.DisposeAsync();
        }


        [Fact]
        public async Task T05_DisposeDisposedSocket()
        {
            Connect();
            Socket.Dispose();
            await Assert.ThrowsAnyAsync<Exception>(async() => await Disposer.DisposeAsync());
        }
    }
}
