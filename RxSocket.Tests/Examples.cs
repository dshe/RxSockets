using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Xunit.Abstractions;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;

namespace RxSocket.Tests
{
    public class Examples
    {
        private IPEndPoint EndPoint = new IPEndPoint(IPAddress.Loopback, Utility.NetworkUtility.GetRandomUnusedPort());

        private readonly Action<string> Write;
        public Examples(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public async Task T00_SendAndReceiveStringMessage()
        {
            // Create a socket server on the endpoint.
            var server = RxSocketServer.Create(EndPoint);

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            (SocketError error, IRxSocket client) = await RxSocket.ConnectAsync(EndPoint);
            Assert.Equal(SocketError.Success, error);

            // Get the client socket accepted buy the server.
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveObservable.ToStrings().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToBytes());
            Assert.Equal("Welcome!", await dataTask);

            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());
        }

        [Fact]
        public async Task T01_ReceiveObservable()
        {
            var server = RxSocketServer.Create(EndPoint);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            (SocketError error, IRxSocket client) = await RxSocket.ConnectAsync(EndPoint);
            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            var subscription = client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Write(str);
            });

            accept.Send("Welcome!".ToBytes());
            "Welcome Again!".ToBytes().SendTo(accept); // Note SendTo() extension method.

            subscription.Dispose();
            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());
        }

        [Fact]
        public async Task T20_AcceptObservable()
        {
             var server = RxSocketServer.Create(EndPoint);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToBytes().SendTo(accepted);
            });

            (SocketError error1, IRxSocket client1) = await RxSocket.ConnectAsync(EndPoint);
            (SocketError error2, IRxSocket client2) = await RxSocket.ConnectAsync(EndPoint);
            (SocketError error3, IRxSocket client3) = await RxSocket.ConnectAsync(EndPoint);

            Assert.Equal("Welcome!", await client1.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client2.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.Equal("Welcome!", await client3.ReceiveObservable.ToStrings().Take(1).FirstAsync());

            await Task.WhenAll(client1.DisconnectAsync(), client2.DisconnectAsync(), client3.DisconnectAsync(), server.DisconnectAsync());
        }

        [Fact]
        public async Task T30_Both()
        {
            var server = RxSocketServer.Create(EndPoint);

            server.AcceptObservable.Subscribe(accepted =>
            {
                "Welcome!".ToBytes().SendTo(accepted);
                accepted
                    .ReceiveObservable
                    .ToStrings()
                    .Subscribe(s => s.ToBytes().SendTo(accepted));
            });

            var clients = Enumerable.Range(1, 100)
                .Select(_ => RxSocket.ConnectAsync(EndPoint).Result.rxsocket)
                .ToList();

            clients.ForEach(c => c.Send("Hello".ToBytes()));

            foreach (var client in clients)
                Assert.Equal("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());
        }

    }
}
