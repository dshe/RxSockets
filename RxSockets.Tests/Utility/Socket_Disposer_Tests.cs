﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class Socket_Disposer_Tests: TestBase
    {
        private readonly CancellationTokenSource Cts = new();
        public Socket_Disposer_Tests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T01_Dispose_Not_Connected_Socket()
        {
            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, Cts, Logger, "?");
            await disposer.DisposeAsync();
            Assert.True(disposer.DisposeRequested);
            Assert.False(socket.Connected);
        }

        [Fact]
        public async Task T02_Dispose_Connected_Socket()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            var serverDisposer = new SocketDisposer(serverSocket, Cts, Logger, "?");
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var clientSocket = Utilities.CreateSocket();
            var clientDisposer = new SocketDisposer(clientSocket, Cts, Logger, "?");
            clientSocket.Connect(ipEndPoint);
            Assert.False(clientDisposer.DisposeRequested);
            Assert.True(clientSocket.Connected);

            await clientDisposer.DisposeAsync();

            Assert.True(clientDisposer.DisposeRequested);
            Assert.False(clientSocket.Connected);

            Assert.False(serverDisposer.DisposeRequested);
            Assert.False(serverSocket.Connected);

            await serverDisposer.DisposeAsync();

            Assert.True(serverDisposer.DisposeRequested);
            Assert.False(serverSocket.Connected);
        }

        [Fact]
        public async Task T04_Dispose_Multi()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, Cts, Logger, "?");
            socket.Connect(ipEndPoint);
            Assert.True(socket.Connected);
            Assert.False(disposer.DisposeRequested);

            var disposeTasks = Enumerable.Range(1, 8).Select((_) => disposer.DisposeAsync().AsTask()).ToList();
            await Task.WhenAll(disposeTasks);
            Assert.True(disposer.DisposeRequested);
        }

        [Fact]
        public async Task T05_Dispose_Disposed_Socket()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(ipEndPoint);
            serverSocket.Listen(10);

            var socket = Utilities.CreateSocket();
            var disposer = new SocketDisposer(socket, Cts, Logger, "?");

            socket.Connect(ipEndPoint);
            Assert.True(socket.Connected);
            Assert.False(disposer.DisposeRequested);

            socket.Dispose();
            await disposer.DisposeAsync();
        }
    }
}
