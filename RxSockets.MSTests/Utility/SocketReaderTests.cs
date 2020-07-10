using System;
using System.Linq;
using System.Reactive.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class SocketReaderTests : TestBase, IDisposable
    {
        private readonly Socket ServerSocket = Utilities.CreateSocket();
        private readonly Socket Socket = Utilities.CreateSocket();
        public void Dispose()
        {
            ServerSocket.Close();
            Socket.Close();
        }

        [TestMethod]
        public void T01_Disconnect()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            Socket.Connect(endPoint);

            var accepted = ServerSocket.Accept();
            accepted.Disconnect(false);

            byte[] buffer = new byte[10];
            int bytes = Socket.Receive(buffer, SocketFlags.None);
            // after the remote socket disconnects, Socket.Receive() returns 0 bytes
            Assert.IsTrue(bytes == 0);
        }

        [TestMethod]
        public async Task T02_DisconnectReadByteAsync()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            Socket.Connect(endPoint);

            var accepted = ServerSocket.Accept();
            accepted.Disconnect(false);

            var reader = new SocketReader(Socket, "?", default, Logger);

            // after the remote socket disconnects, reader.ReadByteAsync() returns nothing
            var empty = await reader.ReadBytesAsync().IsEmptyAsync();
            Assert.IsTrue(empty);
        }

        [TestMethod]
        public async Task T03_DisconnectSocketReader()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            Socket.Connect(endPoint);
            var accepted = ServerSocket.Accept();

            var reader = new SocketReader(Socket, "?", default, Logger);
            var observable = reader.ReceiveObservable;
            accepted.Close();

            // after the remote socket disconnects, the observable completes
            var result = await observable.SingleOrDefaultAsync();
            Assert.AreEqual(0, result); // default
        }

        [TestMethod]
        public async Task T04_DisconnectAndSend()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            Socket.Connect(endPoint);
            var accepted = ServerSocket.Accept();
            Assert.IsTrue(Socket.Connected);
            Assert.IsTrue(accepted.Connected);

            accepted.Close();

            Socket.Send(new byte[1] {1});

            await Task.Delay(10);

            // after the remote socket disconnects, Send() throws on second usage
            Assert.ThrowsException<SocketException>(() => Socket.Send(new byte[1] { 1 }));
        }

        [TestMethod]
        public async Task T05_Read()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            ServerSocket.Bind(endPoint);
            ServerSocket.Listen(10);
            Socket.Connect(endPoint);
            var accepted = ServerSocket.Accept();
            accepted.Send(new byte[] { 1 });

            var reader = new SocketReader(Socket, "?", default, Logger);
            var observable = reader.ReceiveObservable;

            var result = await observable.FirstAsync();
            Assert.AreEqual(1, result);

            accepted.Close();
        }

    }
}
