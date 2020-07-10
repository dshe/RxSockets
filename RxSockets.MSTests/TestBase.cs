using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MXLogger;
using System.IO;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace RxSockets.MSTests
{
    public abstract class TestBase
    {
        public TestContext? TestContext { get; set; }
        protected readonly ILogger Logger, SocketServerLogger, SocketClientLogger;

        public TestBase()
        {
            var loggerFactory = new LoggerFactory().AddMXLogger(s => TestContext!.WriteLine(s));
            Logger = loggerFactory.CreateLogger("Test");
            SocketServerLogger = loggerFactory.CreateLogger("RxSocketServer");
            SocketClientLogger = loggerFactory.CreateLogger("RxSocketClient");
        }

        [TestCleanup]
        public void TCleanup()
        {
            //Logger.LogInformation($"Result: {TestContext!.CurrentTestOutcome}.");
        }

        [AssemblyCleanup]
        public void ACleanup()
        {
            //Directory.Delete(TestContext!.TestDir, recursive: true);
        }
    }
}