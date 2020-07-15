using System.Threading.Tasks;
using System.Net;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Linq;
using Xunit;

namespace RxSockets.xUnitTests
{
    public class RxSocket_Client_Server_Test : TestBase
    {
        public RxSocket_Client_Server_Test(ITestOutputHelper output) : base(output)  {}

        [Fact]
        public async Task T01_Handshake()
        {
            var server = new RxSocketServer(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var task = Task.Run(async () =>
            {
                var accept = await server.AcceptObservable.FirstAsync();

                var message1 = await accept.ReadAsync().ReadStringAsync();
                Assert.Equal("API", message1);

                var message2 = await accept.ReadAsync().ReadStringsFromBufferWithLengthPrefixAsync();
                Assert.Equal("HelloFromClient", message2.Single());

                accept.Send(new[] { "HelloFromServer" }.ToBufferWithLengthPrefix());

                await server.DisposeAsync();
            });

            // give some time for the server to start
            await Task.Delay(50);

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            // Send only the first message without prefix.
            client.Send("API".ToBuffer());

            // Start sending and receiving messages with an int32 message length prefix (UseV100Plus).
            client.Send(new[] { "HelloFromClient" }.ToBufferWithLengthPrefix());

            var message3 = await client.ReadAsync().ReadStringsFromBufferWithLengthPrefixAsync();
            Assert.Equal("HelloFromServer", message3.Single());

            await client.DisposeAsync();
        }
    }
}
