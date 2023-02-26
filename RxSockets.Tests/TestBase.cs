using Microsoft.Extensions.Logging;

namespace RxSockets.Tests;

public abstract class TestBase
{
    protected readonly Action<string> Write;
    protected readonly ILogger Logger, SocketServerLogger, SocketClientLogger;

    protected TestBase(ITestOutputHelper output)
    {
        Write = output.WriteLine;

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder
            .AddMXLogger(Write)
            .SetMinimumLevel(LogLevel.Debug));

        Logger = loggerFactory.CreateLogger("Test");

        SocketServerLogger = loggerFactory.CreateLogger("RxSocketServer");
        SocketClientLogger = loggerFactory.CreateLogger("RxSocketClient");
    }
}
