using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class SocketConnectorTest : TestBase
    {
        [TestMethod]
        public async Task T01_Connection_Refused()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var e = await Assert.ThrowsExceptionAsync<SocketException>(async () => await SocketConnector.ConnectAsync(endPoint, Logger));
            Assert.AreEqual((int)SocketError.ConnectionRefused, e.ErrorCode);
        }

        [TestMethod]
        public async Task T02_Timeout()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var e = await Assert.ThrowsExceptionAsync<SocketException>(async () => await SocketConnector.ConnectAsync(endPoint, Logger, 1));
            Assert.AreEqual((int)SocketError.TimedOut, e.ErrorCode);
        }

        [TestMethod]
        public async Task T03_Cancellation()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var ct = new CancellationToken(true);
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async() =>
               await SocketConnector.ConnectAsync(endPoint, Logger, -1, ct));
        }

        [TestMethod]
        public async Task T10_Success()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);

            var socket = await SocketConnector.ConnectAsync(endPoint, Logger);
            Assert.IsTrue(socket.Connected);

            socket.Close();
            serverSocket.Dispose();
        }
    }
}

