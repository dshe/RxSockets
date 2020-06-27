using Microsoft.Extensions.Logging;
using System;
using System.Net;
using Xunit.Abstractions;
using MXLogger;

namespace RxSockets.Tests
{
    public abstract class TestBase
    {
        protected readonly IPEndPoint IPEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        protected readonly Action<string> Write;
        protected readonly MXLoggerProvider LoggerProvider;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected readonly ILogger<RxSocketServer> SocketServerLogger;
        protected readonly ILogger<RxSocketClient> SocketClientLogger;

        protected TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
            LoggerProvider = new MXLoggerProvider(output.WriteLine);
            LoggerFactory = new LoggerFactory(new[] { LoggerProvider });

            Logger = LoggerFactory.CreateLogger<TestBase>();
            SocketServerLogger = LoggerFactory.CreateLogger<RxSocketServer>();
            SocketClientLogger = LoggerFactory.CreateLogger<RxSocketClient>();

            /*
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs args) =>
            {
                Debug.Write("CurrentDomain_UnhandledException");
                Write("CurrentDomain_UnhandledException");
                if (args.ExceptionObject is Exception exception)
                    Write(exception.ToString());
            };
            */
        }
    }
}
