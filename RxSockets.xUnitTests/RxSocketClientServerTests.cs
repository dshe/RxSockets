using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Linq;
using Xunit;

namespace RxSockets.xUnitTests
{
    public class RxSocketClientServerTest : TestBase
    {
        public RxSocketClientServerTest(ITestOutputHelper output) : base(output)  {}

        [Fact]
        public async Task T01_Handshake()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            NewThreadScheduler.Default.Schedule(async () =>
            {
                var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
                var accept = await server.AcceptObservable.FirstAsync();

                var message1 = await accept.ReadBytesAsync().ReadStringAsync();
                Assert.Equal("API", message1);

                var message2 = await accept.ReadBytesAsync().ReadStringsAsync();
                Assert.Equal("HelloFromClient", message2.Single());

                accept.Send(new[] { "HelloFromServer" }.ToByteArrayWithLengthPrefix());

                await server.DisposeAsync();
            });

            // give some time for the server to start
            await Task.Delay(100);

            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            // Send only the first message without prefix.
            client.Send("API".ToByteArray());

            // Start sending and receiving messages with an int32 message length prefix (UseV100Plus).
            client.Send(new[] { "HelloFromClient" }.ToByteArrayWithLengthPrefix());

            var message3 = await client.ReadBytesAsync().ReadStringsAsync();
            Assert.Equal("HelloFromServer", message3.Single());

            await client.DisposeAsync();
        }
    }
}
