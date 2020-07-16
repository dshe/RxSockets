using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace RxSockets.xUnitTests
{
    public class Performance_Test1 : TestBase
    {
        public Performance_Test1(ITestOutputHelper output) : base(output) { }
        const int messages = 100_000;

        [Fact]
        public async Task T01_ReceiveStrings()
        {
            var server = RxSocketServer.Create();
            var endPoint = server.IPEndPoint;

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await endPoint.ConnectRxSocketClientAsync();
            var countTask = client.ReceiveObservable.ToStrings().Count().ToTask();
            var accept = await acceptTask;

            var watch = new Stopwatch();
            watch.Start();

            // send messages from server to client
            var message = "Welcome!".ToBuffer();
            for (var i = 0; i < messages; i++)
                accept.Send(message);

            // end countTask
            await accept.DisposeAsync();
            var count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = Stopwatch.Frequency * messages / watch.ElapsedTicks;

            Write($"{frequency:N0} messages / second");

            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
    public class Performance_Test2 : TestBase
    {
        public Performance_Test2(ITestOutputHelper output) : base(output) { }
        const int messages = 100_000;

        [Fact]
        public async Task T02_ReceiveStringsFromPrefixedBytes()
        {
            var server = RxSocketServer.Create();
            var endPoint = server.IPEndPoint;
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            var client = await endPoint.ConnectRxSocketClientAsync();
            Assert.True(client.Connected);

            var countTask = client.ReceiveObservable.RemoveLengthPrefix().ToStrings().Count().ToTask();

            var accept = await acceptTask;
            Assert.True(accept.Connected);

            var message = new [] { "Welcome!" }.ToBufferWithLengthPrefix();

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
