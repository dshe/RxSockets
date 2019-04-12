using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

#nullable enable

namespace RxSockets.Tests
{
    public class Examples : BaseTest
    {
        private readonly IPEndPoint IPEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();

        public Examples(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task T00_Example()
        {
            // Create a socket server on the Endpoint.
            var server = RxSocketServer.Create(IPEndPoint, LoggerFactory);

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
            (IRxSocketClient? client, SocketError error) = await RxSocketClient.ConnectAsync(IPEndPoint, LoggerFactory);
            if (client == null)
                throw new SocketException((int)error);

            client.ReceiveObservable.ToStrings().Subscribe(onNext:message =>
            {
                Assert.Equal("Hello!", message);
            });

            client.Send("Hello!".ToByteArray());

            await Task.Delay(100);

            await client.DisconnectAsync();
        }

        [Fact]
        public async Task T00_SendAndReceiveStringMessage()
        {
            // Create a socket server on the endpoint.
            var server = RxSocketServer.Create(IPEndPoint, LoggerFactory);

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var (client, error) = await RxSocketClient.ConnectAsync(IPEndPoint);
            if (client == null)
                throw new SocketException((int)error);

            // Get the client socket accepted by the server.
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveObservable.ToStrings().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToByteArray());
            Assert.Equal("Welcome!", await dataTask);

            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());
        }

        [Fact]
        public async Task T10_ReceiveObservable()
        {
            var server = RxSocketServer.Create(IPEndPoint, LoggerFactory);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var (client, error) = await RxSocketClient.ConnectAsync(IPEndPoint);
            if (client == null)
                throw new SocketException((int)error);

            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            var subscription = client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToByteArray());
            "Welcome Again!".ToByteArray().SendTo(accept); // Note SendTo() extension method.

            subscription.Dispose();
            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());
        }

        [Fact]
        public async Task T20_AcceptObservable()
        {
            var disconnectables = new List<IAsyncDisconnectable>();

            var server = RxSocketServer.Create(IPEndPoint, LoggerFactory, 10).AddDisconnectableTo(disconnectables);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendTo(accepted);
                disconnectables.Add(accepted);
            });

            var (client1, _) = await RxSocketClient.ConnectAsync(IPEndPoint);
            var (client2, _) = await RxSocketClient.ConnectAsync(IPEndPoint);
            var (client3, _) = await RxSocketClient.ConnectAsync(IPEndPoint);
            disconnectables.AddRange(new[] { client1, client2, client3 });

            Assert.Equal("Welcome!", await client1!.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client2!.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client3!.ReceiveObservable.ToStrings().Take(1).FirstAsync());

            var tasks = disconnectables.Select(d => d.DisconnectAsync());
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task T30_Both()
        {
            List<IAsyncDisconnectable> disconnectables = new List<IAsyncDisconnectable>();

            var server = RxSocketServer.Create(IPEndPoint, LoggerFactory).AddDisconnectableTo(disconnectables);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToByteArray().SendTo(accepted);

                accepted
                    .AddDisconnectableTo(disconnectables)
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => s.ToByteArray().SendTo(accepted));
            });

            List<IRxSocketClient> clients = new List<IRxSocketClient>();
            for (var i = 0; i < 100; i++)
            {
                var(client, _) = await RxSocketClient.ConnectAsync(IPEndPoint);
                client!.Send("Hello".ToByteArray());
                clients.Add(client);
                disconnectables.Add(client);
            }

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());

            var tasks = disconnectables.Select(d => d.DisconnectAsync());
            await Task.WhenAll(tasks);
        }
    }
}

