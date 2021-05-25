using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace RxSockets.Tests
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
                acceptClient.ReceiveAllAsync().ToStrings().ToObservable().Subscribe(onNext: message =>
                {
                    // Echo each message received back to the client.
                    acceptClient.Send(message.ToByteArray());
                });
            });

            // Create a socket client by first connecting to the server at the EndPoint.
            var client = await ipEndPoint.CreateRxSocketClientAsync(SocketClientLogger);

            // Start receiving messages from the server.
            client.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().Subscribe(onNext: message =>
            {
                // The message received from the server is "Hello!".
                Assert.Equal("Hello!", message);
            });

            // Send the message "Hello" to the server (which will be echoed back to the client).
            client.Send("Hello!".ToByteArray());

            await Task.Delay(100);

            // Disconnect and dispose.
            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [Fact]
        public async Task T01_Send_And_Receive_String_Message()
        {
            var server = RxSocketServer.Create(SocketServerLogger);
            var ipEndPoint = server.IPEndPoint;

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var client = await ipEndPoint.CreateRxSocketClientAsync(SocketClientLogger);

            // Get the client socket accepted by the server.
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToByteArray());
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
            var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            var accept = await acceptTask;

            var sub = client.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToByteArray());
            accept.Send("Welcome Again!".ToByteArray());

            await Task.Delay(100);

            sub.Dispose();

            await server.DisposeAsync();
            await client.DisposeAsync();
        }

        [Fact]
        public async Task T20_Accept_Observable()
        {
            var s = String.Create<double>(10, 99, (span, state) =>
            {
                //Span<char> xxx;
                span[1] = 's';
                //span.
                return;
            });

            var server = RxSocketServer.Create(SocketServerLogger);
            var endPoint = server.IPEndPoint;

            server.AcceptObservable.Subscribe(accepted => accepted.Send("Welcome!".ToByteArray()));

            var client1 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            var client2 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            var client3 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);

            Assert.Equal("Welcome!", await client1.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client2.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client3.ReceiveAllAsync().ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());

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
                accepted.Send("Welcome!".ToByteArray());
                accepted
                    .ReceiveAllAsync()
                    .ToObservableFromAsyncEnumerable()
                    .ToStrings()
                    .Subscribe(s => accepted.Send(s.ToByteArray()));
            });


            var clients = new List<IRxSocketClient>();
            for (var i = 0; i < 3; i++)
            {
                var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
                client.Send("Hello".ToByteArray());
                clients.Add(client);
            }

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Skip(1).Take(1).FirstAsync());

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
                acceptClient.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToByteArray());
                });
            });

            var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            client.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToByteArray());

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
                acceptClient.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
                {
                    acceptClient.Send(message.ToByteArray());
                });
            });

            var client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            client.ReceiveAllAsync().ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
            {
                Write(message);
            });

            client.Send("Hello!".ToByteArray());
            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await server.DisposeAsync();
            await client.DisposeAsync();
        }
    }
}

