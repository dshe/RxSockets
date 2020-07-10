using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class PerformanceTest1 : TestBase
    {
        public PerformanceTest1(ITestOutputHelper output) : base(output) { }
        const int messages = 100_000;

        [Fact]
        public async Task T01_ReceiveStrings()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer();
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await endPoint.ConnectRxSocketClientAsync();
            var countTask = client.ReceiveObservable.ToStrings().Count().ToTask();
            var accept = await acceptTask;

            var watch = new Stopwatch();
            watch.Start();

            // send messages from server to client
            var message = "Welcome!".ToByteArray();
            for (var i = 0; i < messages; i++)
                accept.Send(message);

            // end count task
            await accept.DisposeAsync();
            var count = await countTask; // index out of range

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");

            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
    public class PerformanceTest2 : TestBase
    {
        public PerformanceTest2(ITestOutputHelper output) : base(output) { }
        const int messages = 100_000;

        [Fact]
        public async Task T02_ReceiveStringsFromPrefixedBytes()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer();
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var client = await endPoint.ConnectRxSocketClientAsync();

            Assert.True(client.Connected);

            var countTask = client.ReceiveObservable.RemoveLengthPrefix().ToStringArray().Count().ToTask();

            var accept = await acceptTask;
            Assert.True(accept.Connected);

            var message = new [] { "Welcome!" }.ToByteArrayWithLengthPrefix();

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < messages; i++)
                accept.Send(message);

            // end count task
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
