using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class RxSocketClientServerTest : TestBase
    {
        [TestMethod]
        public async Task T01_Handshake()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            NewThreadScheduler.Default.Schedule(async () =>
            {
                var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
                var accept = await server.AcceptObservable.FirstAsync();

                var message1 = await accept.ReadBytesAsync().ReadStringAsync();
                Assert.AreEqual("API", message1);

                var message2 = await accept.ReadBytesAsync().ReadStringsAsync();
                Assert.AreEqual("HelloFromClient", message2.Single());

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
            Assert.AreEqual("HelloFromServer", message3.Single());

            await client.DisposeAsync();
        }
    }
}
