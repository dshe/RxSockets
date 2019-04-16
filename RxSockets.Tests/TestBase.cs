using Microsoft.Extensions.Logging;
using System;
using System.Net;
using Xunit.Abstractions;
using Divergic.Logging.Xunit;
using System.Text;

namespace RxSockets.Tests
{
    public abstract class TestBase
    {
        protected readonly IPEndPoint IPEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
        protected readonly Action<string> Write;
        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;
        protected readonly ILogger<RxSocketServer> SocketServerLogger;
        protected readonly ILogger<RxSocketClient> SocketClientLogger;

        protected TestBase(ITestOutputHelper output)
        {
            Write = output.WriteLine;
            // using Divergic.Logging.Xunit;
            LoggerFactory = new LoggerFactory().AddXunit(output, MyFormatter);
            //LoggerFactory = LogFactory.Create(output);
            Logger = LoggerFactory.CreateLogger<TestBase>();
            SocketServerLogger = LoggerFactory.CreateLogger<RxSocketServer>();
            SocketClientLogger = LoggerFactory.CreateLogger<RxSocketClient>();
        }

        private static string MyFormatter(int scopeLevel, string name, LogLevel logLevel, EventId eventId, string message, Exception exception)
        {
            var sb = new StringBuilder();

            if (scopeLevel > 0)
                sb.Append(' ', scopeLevel * 2);

            sb.Append($"{GetShortLogLevelString(logLevel)}   ");

            if (!string.IsNullOrEmpty(name))
            {
                name = name.Substring(name.IndexOf('.')+ 1);
                sb.Append($"{name}   ");
            }

            if (eventId != null && eventId.Id != 0)
                sb.Append($"[{eventId.Id}]: ");

            if (!string.IsNullOrEmpty(message))
                sb.Append(message);

            if (exception != null)
                sb.Append($"\n{exception}");

            return sb.ToString();
        }

        private static string GetShortLogLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:       return "Trace  ";
                case LogLevel.Debug:       return "Debug";
                case LogLevel.Information: return "Info    ";
                case LogLevel.Warning:     return "Warn  ";
                case LogLevel.Error:       return "Error   ";
                case LogLevel.Critical:    return "Critical";
                case LogLevel.None:        return "None ";
                default: throw new Exception("invalid");
            }
        }
    }
}
