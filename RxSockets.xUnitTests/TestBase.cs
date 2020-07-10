using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;
using MXLogger;

namespace RxSockets.xUnitTests
{
    public abstract class TestBase
    {
        protected readonly Action<string> Write;
        protected readonly ILogger Logger, SocketServerLogger, SocketClientLogger;

        protected TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
            var loggerFactory = new LoggerFactory().AddMXLogger(Write);
            Logger = loggerFactory.CreateLogger("Test");
            SocketServerLogger = loggerFactory.CreateLogger("RxSocketServer");
            SocketClientLogger = loggerFactory.CreateLogger("RxSocketClient");
        }
    }
}
