using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class SocketDisposerTest: TestBase
    {
        public SocketDisposerTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T01_DisposeNotConnectedSocket()
        {
            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);
            await disposer.DisposeAsync();
            Assert.True(disposer.DisposeRequested);

            var serverSocket = Utilities.CreateSocket();
            var serverDisposer = new SocketDisposer(serverSocket, "?", Logger);
            await serverDisposer.DisposeAsync();
            Assert.True(serverDisposer.DisposeRequested);
        }

        [Fact]
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
            Assert.True(socket.Connected);

            await disposer.DisposeAsync();
            Assert.True(disposer.DisposeRequested);

            await serverDisposer.DisposeAsync();
            Assert.True(serverDisposer.DisposeRequested);
        }

        [Fact]
        public async Task T04_DisposeMulti()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);
            socket.Connect(ipEndPoint);
            Assert.True(socket.Connected);
            Assert.True(!disposer.DisposeRequested);

            var disposeTasks = Enumerable.Range(1, 8).Select((_) => disposer.DisposeAsync()).ToList();
            await Task.WhenAll(disposeTasks);
            Assert.True(disposer.DisposeRequested);
        }

        [Fact]
        public async Task T05_DisposeDisposedSocket()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, "?", Logger);

            socket.Connect(ipEndPoint);
            Assert.True(socket.Connected);
            Assert.True(!disposer.DisposeRequested);

            socket.Dispose();
            await disposer.DisposeAsync();
        }
    }
}
