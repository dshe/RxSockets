using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class RxSocketServerTest : TestBase
    {
        [TestMethod]
        public void T01_InvalidEndPoint()
        {
            var endPoint = new IPEndPoint(IPAddress.Parse("111.111.111.111"), 1111);
            Assert.ThrowsException<SocketException>(() => endPoint.CreateRxSocketServer(logger: SocketServerLogger));
        }

        [TestMethod]
        public async Task T02_AcceptSuccess()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var clientSocket = Utilities.CreateSocket();
            clientSocket.Connect(endPoint);

            var acceptedSocket = await acceptTask;

            Assert.IsTrue(clientSocket.Connected && acceptedSocket.Connected);

            clientSocket.Disconnect(false);
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T03_DisconnectBeforeAccept()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            await server.DisposeAsync();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await server.AcceptObservable.LastOrDefaultAsync());
            //await server.AcceptObservable.LastOrDefaultAsync();
        }

        [TestMethod]
        public async Task T04_DisconnectWhileAccept()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var acceptTask = server.AcceptObservable.LastAsync().ToTask();
            await server.DisposeAsync();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await acceptTask);
        }
    }
}
