using System;
using System.Net;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Xunit;

namespace RxSockets.xUnitTests
{
    public class Simple_Example
    {
        [Fact]
        public async Task Example()
        {
            // Create a server on the local machine using an available port.
            IRxSocketServer server = RxSocketServer.Create();

            // Find the IPEndPoint of the server.
            IPEndPoint ipEndPoint = server.IPEndPoint;

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                // After the server accepts a client connection, start receiving messages from the client and...
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToBuffer());
                });
            });



            // Create a client by connecting to the server at ipEndPoint.
            IRxSocketClient client = await ipEndPoint.ConnectRxSocketClientAsync();

            // Start receiving messages from the server.
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                // The message received from the server is "Hello!".
                Assert.Equal("Hello!", message);
            });

            // Send the message "Hello" to the server, which the server will then echo back to the client.
            client.Send("Hello!".ToBuffer());

            // Allow time for communication to complete.
            await Task.Delay(10);

            // Disconnect.
            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
}
