using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests;

public class Socket_Acceptor_Tests : TestBase
{
    public Socket_Acceptor_Tests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T00_Success()
    {
        var endPoint = Utilities.CreateIPEndPointOnPortZero();
        var serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);
        endPoint = serverSocket.LocalEndPoint as IPEndPoint ?? throw new InvalidOperationException();

        var task = Task.Run(async () => 
        {
            var acceptor = new SocketAcceptor(serverSocket, SocketServerLogger);
            await foreach(var cli in acceptor.AcceptAllAsync(default))
            {
                Logger.LogDebug("client");
            }
        });

        var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger, default);
        Assert.True(client.Connected);

        await Task.Delay(100);

        await client.DisposeAsync();
    }
}
