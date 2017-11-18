using System;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net;
using RxSocket.Tests.Utility;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Diagnostics;

namespace RxSocket.Tests
{
    public class TestPerformance : IAsyncLifetime
    {
        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());
        private IRxSocketServer server;
        private IRxSocket client, accept;

        private readonly Action<string> Write;
        public TestPerformance(ITestOutputHelper output) =>  Write = output.WriteLine;

        public async Task InitializeAsync()
        {
            server = RxSocketServer.Create(EndPoint);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            client = (await RxSocket.ConnectAsync(EndPoint)).rxsocket;
            accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);
        }

        public async Task DisposeAsync() =>
            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());

        [Fact]
        public async Task T01_ReceiveString()
        {
            var messages = 100_000;

            var message = "Welcome!".ToBytes();

            var watch = new Stopwatch();
            watch.Start();

            var countTask = client.ReceiveObservable.ToStrings().Count().ToTask();

            for (var i = 0; i < messages; i++)
                accept.Send(message);
            await accept.DisconnectAsync();

            var count = await countTask;

            watch.Stop();

            Assert.Equal(messages, count);

            var frequency = messages / watch.ElapsedMilliseconds * 1000D;

            Write($"{frequency} messages / second");
        }

    }
}
