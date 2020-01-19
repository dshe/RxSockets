using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class SocketReaderTests : TestBase, IDisposable
    {
        private readonly Socket ServerSocket = Utilities.CreateSocket();
        private readonly Socket Socket = Utilities.CreateSocket();
        public SocketReaderTests(ITestOutputHelper output) : base(output) { }

        public void Dispose()
        {
            ServerSocket.Close();
            Socket.Close();
        }

        [Fact]
        public void T01_Disconnect()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);

            var accepted = ServerSocket.Accept();
            accepted.Disconnect(false);

            byte[] buffer = new byte[10];
            int bytes = Socket.Receive(buffer, SocketFlags.None);
            // after the remote socket disconnects, Socket.Receive() returns 0 bytes
            Assert.True(bytes == 0);
        }

        [Fact]
        public async Task T02_DisconnectReadByteAsync()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);

            var accepted = ServerSocket.Accept();
            accepted.Disconnect(false);

            var reader = new SocketReader(Socket, "?", default, Logger);

            // after the remote socket disconnects, reader.ReadByteAsync() throws
            var e = await Assert.ThrowsAsync<SocketException>(async() => await reader.ReadByteAsync());
            Assert.Equal(SocketError.NoData, e.SocketErrorCode);
        }

        [Fact]
        public async Task T03_DisconnectSocketReader()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);
            var accepted = ServerSocket.Accept();

            var reader = new SocketReader(Socket, "?", default, Logger);
            var observable = reader.CreateReceiveObservable();
            accepted.Close();

            // after the remote socket disconnects, the observable completes
            var result = await observable.SingleOrDefaultAsync();
            Assert.Equal(0, result); // default
        }

        [Fact]
        public void T04_DisconnectAndSend()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);
            var accepted = ServerSocket.Accept();
            Assert.True(Socket.Connected);
            Assert.True(accepted.Connected);

            accepted.Close();

            Socket.Send(new byte[1] {1});

            // after the remote socket disconnects, Send() throws on second usage
            Assert.Throws<SocketException>(() => Socket.Send(new byte[1] { 1 }));
        }

        [Fact]
        public async Task T05_Read()
        {
            ServerSocket.Bind(IPEndPoint);
            ServerSocket.Listen(10);
            Socket.Connect(IPEndPoint);
            var accepted = ServerSocket.Accept();
            accepted.Send(new byte[] { 1 });

            var reader = new SocketReader(Socket, "?", default, Logger);
            var observable = reader.CreateReceiveObservable();

            var result = await observable.FirstAsync();
            Assert.Equal(1, result);

            accepted.Close();
        }

    }
}
