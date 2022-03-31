using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Linq;
using Xunit;
using System.Reactive.Linq;
using System;
using Microsoft.Extensions.Logging;
namespace RxSockets.Tests;

public class ClientServerTest : TestBase
{
    public ClientServerTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T01_Handshake()
    {
        var server = RxSocketServer.Create(SocketServerLogger);
        var endPoint = server.IPEndPoint;

        server.AcceptAllAsync().ToObservableFromAsyncEnumerable().Subscribe(async acceptClient =>
        {
            var message1 = await acceptClient.ReceiveAllAsync().ToStrings().FirstAsync();
            Assert.Equal("Hello1FromClient", message1);

            acceptClient.Send(new[] { "Hello1FromServer" }.ToByteArray());

            var messages = await acceptClient.ReceiveAllAsync().ToArraysFromBytesWithLengthPrefix().ToStringArrays().FirstAsync();
            Assert.Equal("Hello2FromClient", messages[0]);

            acceptClient.Send(new[] { "Hello2FromServer" }.ToByteArray().ToByteArrayWithLengthPrefix());

            acceptClient.Send(new[] { "Hello3FromServer" }.ToByteArray().ToByteArrayWithLengthPrefix());
        });

        var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);

        // Send the first message without prefix.
        client.Send("Hello1FromClient".ToByteArray());

        // Receive the response message without prefix.
        var message1 = await client.ReceiveAllAsync().ToStrings().FirstAsync();
        Assert.Equal("Hello1FromServer", message1);

        // Start sending and receiving messages with an int32 message length prefix.
        client.Send(new[] { "Hello2FromClient" }.ToByteArray().ToByteArrayWithLengthPrefix());

        var message3 = await client.ReceiveAllAsync().ToArraysFromBytesWithLengthPrefix().ToStringArrays().FirstAsync();
        Assert.Equal("Hello2FromServer", message3.Single());

        client.ReceiveAllAsync()
            .ToArraysFromBytesWithLengthPrefix()
            .ToStringArrays()
            .ToObservableFromAsyncEnumerable()
            .Subscribe(x =>
            {
                Logger.LogInformation(x[0]);
            });

        await Task.Delay(10);

        await client.DisposeAsync();
        await server.DisposeAsync();
    }
}
