using System;
using System.Net;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class Example1
    {
        // Create an IPEndPoint on the local machine on an available arbitrary port.
        public readonly IPEndPoint IPEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 11345);

        [TestMethod]
        public async Task Example()
        {
            // Create a socket server on the IPEndPoint.
            IRxSocketServer server = IPEndPoint.CreateRxSocketServer();

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                // After the server accepts a client connection, start receiving messages from the client and...
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToByteArray());
                });
            });



            // Create a socket client by first connecting to the server at the EndPoint.
            IRxSocketClient client = await IPEndPoint.ConnectRxSocketClientAsync();

            // Start receiving messages from the server.
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                // The message received from the server is "Hello!".
                Assert.AreEqual("Hello!", message);
            });

            // Send the message "Hello" to the server, which the server will then echo back to the client.
            client.Send("Hello!".ToByteArray());

            // Allow time for communication to complete.
            await Task.Delay(50);

            await server.DisposeAsync(); //dispose order?
            await client.DisposeAsync();
        }

    }
}

