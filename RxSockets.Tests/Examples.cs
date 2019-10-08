using System;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;

#nullable enable

namespace RxSockets.Tests
{
    public class Examples : TestBase
    {
        public Examples(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task T00_Example()
        {
            // Create a socket server on the Endpoint.
            var server = IPEndPoint.CreateRxSocketServer();

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToByteArray());
                });
            });

            // Create a socket client by connecting to the server at EndPoint.
            var client = await IPEndPoint.ConnectRxSocketClientAsync();

            client.ReceiveObservable.ToStrings().Subscribe(onNext:message =>
            {
                Assert.Equal("Hello!", message);
            });

            client.Send("Hello!".ToByteArray());

            await Task.Delay(100);

            client.Dispose();
            server.Dispose();
        }

        [Fact]
        public async Task T00_SendAndReceiveStringMessage()
        {
            // Create a socket server on the endpoint.
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            // Get the client socket accepted by the server.
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveObservable.ToStrings().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToByteArray());
            Assert.Equal("Welcome!", await dataTask);

            client.Dispose();
            server.Dispose();
        }

        [Fact]
        public async Task T10_ReceiveObservable()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToByteArray());
            "Welcome Again!".ToByteArray().SendTo(accept); // Note SendTo() extension method.

            client.Dispose();
            server.Dispose();
        }

        [Fact]
        public async Task T20_AcceptObservable()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendTo(accepted);
            });

            var client1 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client2 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client3 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            Assert.Equal("Welcome!", await client1.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client2.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client3.ReceiveObservable.ToStrings().Take(1).FirstAsync());

            client1.Dispose();
            client2.Dispose();
            client3.Dispose();
            server.Dispose();
        }

        [Fact]
        public async Task T30_Both()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);  //.AddDisconnectableTo(disconnectables);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendTo(accepted);

                accepted
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => s.ToByteArray().SendTo(accepted));
            });

            var clients = new List<IRxSocketClient>();
            for (var i = 0; i < 10; i++)
            {
                var client = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
                client.Send("Hello".ToByteArray());
                clients.Add(client);
            }

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());

            foreach (var client in clients)
                client.Dispose();
            server.Dispose();
        }
    }
}

