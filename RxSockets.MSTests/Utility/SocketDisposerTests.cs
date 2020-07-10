using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class SocketDisposerTest: TestBase
    {
        [TestMethod]
        public async Task T01_DisposeNotConnectedSocket()
        {
            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);
            await disposer.DisposeAsync();
            Assert.IsTrue(disposer.DisposeRequested);

            var serverSocket = Utilities.CreateSocket();
            var serverDisposer = new SocketDisposer(serverSocket, "?", Logger);
            await serverDisposer.DisposeAsync();
            Assert.IsTrue(serverDisposer.DisposeRequested);
        }

        [TestMethod]
        public async Task T02_DisposeConnectedSocket()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            var serverDisposer = new SocketDisposer(serverSocket, "?", Logger);
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);
            socket.Connect(ipEndPoint);
            Assert.IsTrue(socket.Connected);

            await disposer.DisposeAsync();
            Assert.IsTrue(disposer.DisposeRequested);

            await serverDisposer.DisposeAsync();
            Assert.IsTrue(serverDisposer.DisposeRequested);
        }

        [TestMethod]
        public async Task T04_DisposeMulti()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);
            socket.Connect(ipEndPoint);
            Assert.IsTrue(socket.Connected);
            Assert.IsTrue(!disposer.DisposeRequested);

            var disposeTasks = Enumerable.Range(1, 8).Select((_) => disposer.DisposeAsync()).ToList();
            await Task.WhenAll(disposeTasks);
            Assert.IsTrue(disposer.DisposeRequested);
        }

        [TestMethod]
        public async Task T05_DisposeDisposedSocket()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);

            socket.Connect(ipEndPoint);
            Assert.IsTrue(socket.Connected);
            Assert.IsTrue(!disposer.DisposeRequested);

            socket.Dispose();
            await disposer.DisposeAsync();
        }
    }
}
