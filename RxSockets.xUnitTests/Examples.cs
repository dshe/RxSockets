using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using System.Threading;

namespace RxSockets.xUnitTests
{
    public class Examples : TestBase
    {
        public Examples(ITestOutputHelper output) : base(output) {}

        [Fact]
        public async Task T00_Example()
        {
            // Create a socket server.
            var server = RxSocketServer.Create(SocketServerLogger);

            // The IPEndPoint is chosen automatically.
            var ipEndPoint = server.IPEndPoint;

            // Start accepting connections from clients.
            server.AcceptObservable.Subscribe(acceptClient =>
            {
                // After the server accepts a client connection...
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToBuffer());
                });
            });

            // Create a socket client by first connecting to the server at the EndPoint.
            var client = await ipEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            // Start receiving messages from the server.
            client.ReceiveObservable.ToStrings().Subscribe(onNext:message =>
            {
                // The message received from the server is "Hello!".
                Assert.Equal("Hello!", message);
            });

            // Send the message "Hello" to the server (which will be echoed back to the client).
            client.Send("Hello!".ToBuffer());

            // Disconnect.
            await server.DisposeAsync();
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T01_Send_And_Receive_String_Message()
        {
            var server = RxSocketServer.Create(SocketServerLogger);
            var ipEndPoint = server.IPEndPoint;

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var client = await ipEndPoint.ConnectRxSocketClientAsync(SocketClientLogger);

            // Get the client socket accepted by the server.
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveObservable.ToStrings().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToBuffer());
            Assert.Equal("Welcome!", await dataTask);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T10_Receive_Observable()
        {
            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var accept = await acceptTask;

            var sub = client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToBuffer());
            accept.Send("Welcome Again!".ToBuffer());

            await Task.Delay(100);

            sub.Dispose();

            await server.DisposeAsync();
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T20_Accept_Observable()
        {
            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            server.AcceptObservable.Subscribe(accepted => accepted.Send("Welcome!".ToBuffer()));

            var client1 = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client2 = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            var client3 = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);

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
            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            server.AcceptObservable.Subscribe(accepted =>
            {
                accepted.Send("Welcome!".ToBuffer());
                accepted
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => accepted.Send(s.ToBuffer()));
            });


            var clients = new List<IRxSocketClient>();
            for (var i = 0; i < 3; i++)
            {
                var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
                client.Send("Hello".ToBuffer());
                clients.Add(client);
            }

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());

            foreach (var client in clients)
                await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T40_Client_Disconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            IRxSocketClient? acceptClient = null;
            server.AcceptObservable.Subscribe(ac =>
            {
                acceptClient = ac;
                semaphore.Release();
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToBuffer());
                });
            });

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToBuffer());

            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await server.DisposeAsync();
            await client.DisposeAsync();

            semaphore.Dispose();
        }

        [Fact]
        public async Task T41_Server_Disconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            IRxSocketClient? acceptClient = null;
            server.AcceptObservable.Subscribe(ac =>
            {
                acceptClient = ac;
                semaphore.Release();
                acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToBuffer());
                });
            });

            var client = await endPoint.ConnectRxSocketClientAsync(SocketClientLogger);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToBuffer());
            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await server.DisposeAsync();
            await client.DisposeAsync();
        }
    }
}

