using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class Socket_Acceptor_Tests : TestBase
    {
        public Socket_Acceptor_Tests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T00_Success()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();
            var serverSocket = Utilities.CreateSocket();
            serverSocket.Bind(endPoint);
            serverSocket.Listen(10);

            var acceptor = new SocketAcceptor(serverSocket, SocketServerLogger);
            var ctsServer = new CancellationTokenSource();
            var observable = acceptor.CreateAcceptObservable(ctsServer.Token);
            var subscription = observable.Subscribe((x) =>
            {
                Logger.LogDebug("client");
            });

            var ctsClient = new CancellationTokenSource();
            var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger, ctsClient.Token);
            Assert.True(client.Connected);

            await Task.Delay(100);
            //subscription.Dispose();
            //Assert.False(client.Connected);

            await client.DisposeAsync();
        }


    }
}
