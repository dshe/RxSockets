using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Threading;
using System.IO;
using System.Linq;

namespace RxSockets.Tests
{
    public class RxSocketClientServerTest : TestBase
    {
        public RxSocketClientServerTest(ITestOutputHelper output) : base(output)  {}

        [Fact]
        public async Task T01_Handshake()
        {
            NewThreadScheduler.Default.Schedule(async () =>
            {
                var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
                var accept = await server.AcceptObservable.FirstAsync();

                var message1 = await accept.ReadBytesAsync().ReadStringAsync();
                Assert.Equal("API", message1);

                var message2 = await accept.ReadBytesAsync().ReadStringsAsync();
                Assert.Equal("HelloFromClient", message2.Single());

                accept.Send(new[] { "HelloFromServer" }.ToByteArrayWithLengthPrefix());

                await server.DisposeAsync();
            });

            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

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
