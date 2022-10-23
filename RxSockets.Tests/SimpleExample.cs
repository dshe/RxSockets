using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RxSockets.Tests;

public class SimpleExample
{
    [Fact]
    public async Task AsyncEnumerable_Example()
    {
        // Create a server on the local machine using a random available port.
        IRxSocketServer server = RxSocketServer.Create();

        Task task = Task.Run(async() =>
        {
            await foreach (IRxSocketClient acceptClient in server.AcceptAllAsync())
            {
                await foreach (string msg in acceptClient.ReceiveAllAsync().ToStrings())
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(msg.ToByteArray());
                }
            }
        });

        // Create a client by connecting to the server.
        IRxSocketClient client = await server.LocalIPEndPoint.CreateRxSocketClientAsync();

        // Send the message "Hello" to the server, which the server will then echo back to the client.
        client.Send("Hello!".ToByteArray());

        // Receive the message from the server.
        string message = await client.ReceiveAllAsync().ToStrings().FirstAsync();
        Assert.Equal("Hello!", message);

        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task Observable_Example()
    {
        // Create a server on the local machine using a random available port.
        IRxSocketServer server = RxSocketServer.Create();

        // Start accepting connections from clients.
        server
            .AcceptAllAsync()
            .ToObservableFromAsyncEnumerable()
            .Subscribe(onNext: acceptClient =>
            {
                // After the server accepts a client connection,
                // start receiving messages from the client and ...
                acceptClient
                    .ReceiveAllAsync()
                    .ToObservableFromAsyncEnumerable()
                    .ToStrings()
                    .Subscribe(onNext: message =>
                    {
                        // Echo each message received back to the client.
                        acceptClient.Send(message.ToByteArray());
                    });
            });

        // Create a client by connecting to the server.
        IRxSocketClient client = await server.LocalIPEndPoint.CreateRxSocketClientAsync();

        // Send the message "Hello" to the server, which the server will then echo back to the client.
        client.Send("Hello!".ToByteArray());

        // Receive the message from the server.
        string message = await client.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().FirstAsync();

        Assert.Equal("Hello!", message);

        await client.DisposeAsync();
        await server.DisposeAsync();
    }
}
