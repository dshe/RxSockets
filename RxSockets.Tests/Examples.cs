using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using System.Threading;

namespace RxSockets.Tests
{
    public class Examples : TestBase
    {
        public Examples(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task T00_Example()
        {
            // Create a socket server on the EndPoint.
            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                // After the server accepts a client connection...
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToByteArray());
                });
            });

            // Create a socket client by first connecting to the server at the EndPoint.
            var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);

            // Start receiving messages from the server.
            client.ReceiveObservable.ToStrings().Subscribe(onNext:message =>
            {
                // The message received from the server is "Hello!".
                Assert.Equal("Hello!", message);
            });

            // Send the message "Hello" to the server (which will be echoed back to the client).
            client.Send("Hello!".ToByteArray());

            await Task.Delay(100);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T01_SendAndReceiveStringMessage()
        {
            // Create a socket server on the endpoint.
            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);

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
            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
            var accept = await acceptTask;

            client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToByteArray());
            accept.Send("Welcome Again!".ToByteArray());

            await Task.Delay(100);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T20_AcceptObservable()
        {
            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted => accepted.Send("Welcome!".ToByteArray()));

            var client1 = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
            var client2 = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
            var client3 = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);

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
            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted =>
            {
                accepted.Send("Welcome!".ToByteArray());
                accepted
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => accepted.Send(s.ToByteArray()));
            });


            var clients = new List<IRxSocketClient>();
            for (var i = 0; i < 3; i++)
            {
                var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
                client.Send("Hello".ToByteArray());
                clients.Add(client);
            }

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());

            foreach (var client in clients)
                await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T40_ClientDisconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);
            IRxSocketClient? acceptClient = null;
            server.AcceptObservable.Subscribe(ac =>
            {
                acceptClient = ac;
                semaphore.Release();
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToByteArray());
                });
            });

            var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToByteArray());

            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await client.DisposeAsync();

            // should throw!
            acceptClient.Send("Anybody there?".ToByteArray());
            //client.Send("Anybody there?".ToByteArray());

            await server.DisposeAsync();
        }

        [Fact]
        public async Task T41_ServerDisconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var server = IPEndPoint.CreateRxSocketServer(10, SocketServerLogger);
            IRxSocketClient? acceptClient = null;
            server.AcceptObservable.Subscribe(ac =>
            {
                acceptClient = ac;
                semaphore.Release();
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToByteArray());
                });
            });

            var client = await IPEndPoint.ConnectRxSocketClientAsync(10, SocketClientLogger);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToByteArray());
            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await server.DisposeAsync();

            // should throw!
            client.Send("Anybody there?".ToByteArray());
            //acceptClient.Send("Anybody there?".ToByteArray());

            await client.DisposeAsync();
        }
    }
}

