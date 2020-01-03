using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

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

            await client.DisposeAsync();
            await server.DisposeAsync();
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

            await client.DisposeAsync();
            await server.DisposeAsync();
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
            "Welcome Again!".ToByteArray().SendFrom(accept); // Note: SendTo() extension method.

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T20_AcceptObservable()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendFrom(accepted);
            });

            var client1 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client2 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client3 = await IPEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            Assert.Equal("Welcome!", await client1.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client2.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client3.ReceiveObservable.ToStrings().Take(1).FirstAsync());

            await client1.DisposeAsync();
            await client2.DisposeAsync();
            await client3.DisposeAsync();

            await server.DisposeAsync();
        }

        [Fact]
        public async Task T30_Both()
        {
            var server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendFrom(accepted);

                accepted
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => s.ToByteArray().SendFrom(accepted));
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
                await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }
}

