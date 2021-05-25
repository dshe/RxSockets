using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.Tests
{
    public class PerformanceTest1 : TestBase
    {
        public PerformanceTest1(ITestOutputHelper output) : base(output) { }
        const int numberOfMessages = 100_000;

        [Fact]
        public async Task T01_ReceiveStrings()
        {
            var server = RxSocketServer.Create();
            var endPoint = server.IPEndPoint;

            var acceptFirstClientTask = server.AcceptObservable.FirstAsync();
            var client = await endPoint.CreateRxSocketClientAsync();
            var acceptClient = await acceptFirstClientTask;
            var countTask = acceptClient.ReceiveAllAsync().ToStrings().CountAsync();

            var watch = new Stopwatch();
            watch.Start();

            // send messages from server to client
            var message = "Welcome!".ToByteArray();
            for (var i = 0; i < numberOfMessages; i++)
                client.Send(message);

            // end countTask
            await client.DisposeAsync();
            var count = await countTask;

            watch.Stop();

            Assert.Equal(numberOfMessages, count);

            var frequency = Stopwatch.Frequency * numberOfMessages / watch.ElapsedTicks;
            Write($"{frequency:N0} messages / second");

            await server.DisposeAsync();
        }
    }

    public class PerformanceTest2 : TestBase
    {
        public PerformanceTest2(ITestOutputHelper output) : base(output) { }
        const int numberOfMessages = 100_000;

        [Fact]
        public async Task T02_ReceiveStringsFromPrefixedBytes()
        {
            var server = RxSocketServer.Create();
            var endPoint = server.IPEndPoint;
            var acceptFirstClientTask = server.AcceptObservable.FirstAsync();

            var client = await endPoint.CreateRxSocketClientAsync();
            Assert.True(client.Connected);

            var countTask = client.ReceiveAllAsync().ToArraysFromBytesWithLengthPrefix().ToStringArrays().CountAsync();

            var acceptClient = await acceptFirstClientTask;
            Assert.True(acceptClient.Connected);

            var message = new [] { "Welcome!" }.ToByteArray().ToByteArrayWithLengthPrefix();

            var watch = new Stopwatch();
            watch.Start();

            for (var i = 0; i < numberOfMessages; i++)
                acceptClient.Send(message);

            // end count task
            await acceptClient.DisposeAsync();
            int count = await countTask;

            watch.Stop();
            Assert.Equal(numberOfMessages, count);

            var frequency = Stopwatch.Frequency * numberOfMessages / watch.ElapsedTicks;
            Write($"{frequency:N0} messages / second");

            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
}
