using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace RxSockets.Tests;

public class ClientServerTest : TestBase
{
    public ClientServerTest(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T01_Handshake()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);

        server.AcceptAllAsync.ToObservableFromAsyncEnumerable()
            .Select(acceptClient => Observable.FromAsync(async ct =>
            {
                string message1 = await acceptClient.ReceiveAllAsync.ToStrings().FirstAsync();
                Assert.Equal("Hello1FromClient", message1);

                acceptClient.Send(new[] { "Hello1FromServer" }.ToByteArray());

                string[] messages = await acceptClient.ReceiveAllAsync.ToArraysFromBytesWithLengthPrefix().ToStringArrays().FirstAsync();
                Assert.Equal("Hello2FromClient", messages[0]);

                acceptClient.Send(new[] { "Hello2FromServer" }.ToByteArray().ToByteArrayWithLengthPrefix());

                acceptClient.Send(new[] { "Hello3FromServer" }.ToByteArray().ToByteArrayWithLengthPrefix());
            }))
            .Concat()
            .Subscribe();
            
        IRxSocketClient client = await server.LocalEndPoint.CreateRxSocketClientAsync(SocketClientLogger);

        // Send the first message without prefix.
        client.Send("Hello1FromClient".ToByteArray());

        // Receive the response message without prefix.
        string message1 = await client.ReceiveAllAsync.ToStrings().FirstAsync();
        Assert.Equal("Hello1FromServer", message1);

        // Start sending and receiving messages with an int32 message length prefix.
        client.Send(new[] { "Hello2FromClient" }.ToByteArray().ToByteArrayWithLengthPrefix());

        string[] message3 = await client.ReceiveAllAsync.ToArraysFromBytesWithLengthPrefix().ToStringArrays().FirstAsync();
        Assert.Equal("Hello2FromServer", message3.Single());

        client.ReceiveAllAsync
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
