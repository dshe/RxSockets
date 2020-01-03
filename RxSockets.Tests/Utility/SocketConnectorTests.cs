using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class SocketConnectorTest : TestBase
    {
        public SocketConnectorTest(ITestOutputHelper output): base(output) { }

        [Fact]
        public async Task T01_Connection_Refused()
        {
            var e = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(IPEndPoint, Logger));
            Assert.Equal((int)SocketError.ConnectionRefused, e.ErrorCode);
        }

        [Fact]
        public async Task T03_Timeout()
        {
            var e = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(IPEndPoint, Logger, 1));
            Assert.Equal((int)SocketError.TimedOut, e.ErrorCode);
        }

        [Fact]
        public async Task T04_Cancellation()
        {
            var ct = new CancellationToken(true);
            await Assert.ThrowsAsync<OperationCanceledException>(async() =>
               await SocketConnector.ConnectAsync(IPEndPoint, Logger, -1, ct));
        }

        [Fact]
        public async Task T99_Success()
        {
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(IPEndPoint);
            serverSocket.Listen(10);

            var socket = await SocketConnector.ConnectAsync(IPEndPoint, Logger);
            var disposer = new SocketDisposer(socket, Logger);
            await disposer.DisposeAsync();

            serverSocket.Dispose();
        }

    }

}

