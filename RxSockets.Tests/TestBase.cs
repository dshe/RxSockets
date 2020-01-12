using Microsoft.Extensions.Logging;
using System;
using System.Net;
using Xunit.Abstractions;
using System.Text;
using MXLogger;
using System.Diagnostics;

namespace RxSockets.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly Action<string> Write;
        protected readonly IPEndPoint IPEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected readonly ILogger<RxSocketServer> SocketServerLogger;
        protected readonly ILogger<RxSocketClient> SocketClientLogger;
        protected readonly MXLoggerProvider LoggerProvider;

        protected TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;

            //LoggerProvider = new MXLoggerProvider(output.WriteLine);
            LoggerProvider = new MXLoggerProvider();
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

        public void Dispose() => LoggerProvider.WriteTo(Write);
    }
}
