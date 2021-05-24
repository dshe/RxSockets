using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RxSockets.Tests
{
    public class SimpleExample
    {
        [Fact]
        public async Task Example()
        {
            // Create a server on the local machine using an available port.
            IRxSocketServer server = RxSocketServer.Create();

            // Find the address of the server.
            var ipEndPoint = server.IPEndPoint;

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                // After the server accepts a client connection, start receiving messages from the client and ...
                acceptClient.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Subscribe(message =>
                {
                    // Echo each string received back to the client.
                    acceptClient.Send(message.ToByteArray());
                });
            });

            // Create a client by connecting to the server.
            IRxSocketClient client = await ipEndPoint.CreateRxSocketClientAsync();

            // Send the message "Hello" to the server, which the server will then echo back to the client.
            client.Send("Hello!".ToByteArray());

            // Receive the message from the server.
            string message = await client.ReceiveAllAsync().ToStrings().FirstAsync();
            Assert.Equal("Hello!", message);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
}

