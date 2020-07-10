using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class SocketConnectorTest : TestBase
    {
        public SocketConnectorTest(ITestOutputHelper output): base(output) { }

        [Fact]
        public async Task T01_Connection_Refused()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var e = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(endPoint, Logger));
            Assert.Equal((int)SocketError.ConnectionRefused, e.ErrorCode);
        }

        [Fact]
        public async Task T02_Timeout()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var e = await Assert.ThrowsAsync<SocketException>(async () => await SocketConnector.ConnectAsync(endPoint, Logger, 1));
            Assert.Equal((int)SocketError.TimedOut, e.ErrorCode);
        }

        [Fact]
        public async Task T03_Cancellation()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var ct = new CancellationToken(true);
            await Assert.ThrowsAsync<OperationCanceledException>(async() =>
               await SocketConnector.ConnectAsync(endPoint, Logger, -1, ct));
        }

        [Fact]
        public async Task T10_Success()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);

            var socket = await SocketConnector.ConnectAsync(endPoint, Logger);
            Assert.True(socket.Connected);

            socket.Close();
            serverSocket.Dispose();
        }
    }
}

