using Microsoft.Extensions.Logging;
using System;
using System.Net;
using Xunit.Abstractions;
using System.Text;
using MXLogger;

namespace RxSockets.Tests
{
    public abstract class TestBase
    {
        protected readonly Action<string> Write;
        protected readonly IPEndPoint IPEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected readonly ILogger<RxSocketServer> SocketServerLogger;
        protected readonly ILogger<RxSocketClient> SocketClientLogger;

        protected TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
            var provider = new MXLoggerProvider(output.WriteLine);
            LoggerFactory = new LoggerFactory(new[] { provider });

            Logger = LoggerFactory.CreateLogger<TestBase>();
            SocketServerLogger = LoggerFactory.CreateLogger<RxSocketServer>();
            SocketClientLogger = LoggerFactory.CreateLogger<RxSocketClient>();
        }
    }
}
