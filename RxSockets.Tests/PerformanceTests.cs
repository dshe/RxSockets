using System;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;

namespace RxSockets.Tests
{
    public class PerformanceTest : TestBase
    {
        public PerformanceTest(ITestOutputHelper output) : base(output) {}
        const int messages = 100_000;

        [Fact]
        public async Task T01_ReceiveStrings()
        {
            var server = IPEndPoint.CreateRxSocketServer();
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await IPEndPoint.ConnectRxSocketClientAsync();

            Assert.True(client.Connected);
            var countTask = client.ReceiveObservable.ToStrings().Count().ToTask();

            var accept = await acceptTask;
            Assert.True(accept.Connected);

            var watch = new Stopwatch();
            watch.Start();

            // send messages from server to client
            var message = "Welcome!".ToByteArray();
            for (var i = 0; i < messages; i++)
                accept.Send(message);

            await accept.DisposeAsync();

            var count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T02_ReceiveStringsFromPrefixedBytes()
        {
            //var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
            var server = IPEndPoint.CreateRxSocketServer();
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            //var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client = await IPEndPoint.ConnectRxSocketClientAsync();

            Assert.True(client.Connected);

            var countTask = client.ReceiveObservable.FromByteArrayWithLengthPrefix().ToStringArray().Count().ToTask();

            var accept = await acceptTask;
            Assert.True(accept.Connected);

            var message = new [] { "Welcome!" }.ToByteArrayWithLengthPrefix();

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < messages; i++)
                accept.Send(message);

            await accept.DisposeAsync();

            int count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");

            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
}
