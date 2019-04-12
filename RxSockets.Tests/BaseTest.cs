using Microsoft.Extensions.Logging;
using System;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public abstract class BaseTest
    {
        protected readonly Action<string> Write;
        protected readonly ILoggerFactory LoggerFactory; // for injection

        protected BaseTest(ITestOutputHelper output)
        {
            Write = output.WriteLine;
            LoggerFactory = output.ToLoggerFactory();
        }
    }
}
