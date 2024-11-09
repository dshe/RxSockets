using Microsoft.Extensions.Logging;
namespace RxSockets.Tests;

public abstract class TestBase
{
    protected readonly Action<string> Write;
    protected readonly ILoggerFactory LogFactory;
    protected readonly ILogger Logger;

    protected TestBase(ITestOutputHelper output)
    {
        Write = output.WriteLine;

        LogFactory = LoggerFactory.Create(builder => builder
            .AddMXLogger(Write)
            .SetMinimumLevel(LogLevel.Debug));

        Logger = LogFactory.CreateLogger("Test");
    }
}
