using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Logging;

namespace RxSockets.MSTests
{
    [TestClass]
    public class Examples : TestBase
    {
        [TestMethod]
        public async Task T00_Example()
        {
            var ipEndPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            // Create a socket server on the EndPoint.
            var server = ipEndPoint.CreateRxSocketServer(logger: SocketServerLogger);

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
            var client = await ipEndPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            // Start receiving messages from the server.
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                // The message received from the server is "Hello!".
                Assert.AreEqual("Hello!", message);
            });

            // Send the message "Hello" to the server (which will be echoed back to the client).
            client.Send("Hello!".ToByteArray());

            await Task.Delay(10);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T01_SendAndReceiveStringMessage()
        {
            //var ipEndPoint = Utilities.GetEndPointOnLoopbackRandomPort();
            var ipEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, 7530);

            // Create a socket server on the endpoint.
            var server = ipEndPoint.CreateRxSocketServer(logger: SocketServerLogger, backLog: 10);

            // Start a task to allow the server to accept the next client connection.
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            // Create a socket client by successfully connecting to the server at EndPoint.
            var client = await ipEndPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            // Get the client socket accepted by the server.
            var accept = await acceptTask;
            Assert.IsTrue(accept.Connected && client.Connected);

            // start a task to receive the first string from the server.
            var dataTask = client.ReceiveObservable.ToStrings().FirstAsync().ToTask();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToByteArray());
            Assert.AreEqual("Welcome!", await dataTask);

            await Task.Delay(10);

            await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T10_ReceiveObservable()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();
            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var accept = await acceptTask;

            client.ReceiveObservable.ToStrings().Subscribe(str =>
            {
                Logger.LogInformation(str);
            });

            accept.Send("Welcome!".ToByteArray());
            accept.Send("Welcome Again!".ToByteArray());

            await Task.Delay(10);

            await server.DisposeAsync();
            await client.DisposeAsync();
        }

        [TestMethod]
        public async Task T20_AcceptObservable()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);

            server.AcceptObservable.Subscribe(accepted => accepted.Send("Welcome!".ToByteArray()));

            var client1 = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var client2 = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);
            var client3 = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger);

            Assert.AreEqual("Welcome!", await client1.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.AreEqual("Welcome!", await client2.ReceiveObservable.ToStrings().Take(1).FirstAsync());
            Assert.AreEqual("Welcome!", await client3.ReceiveObservable.ToStrings().Take(1).FirstAsync());

            await client1.DisposeAsync();
            await client2.DisposeAsync();
            await client3.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T30_Both()
        {
            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger, backLog: 10);

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
                var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger, timeout: 10);
                client.Send("Hello".ToByteArray());
                clients.Add(client);
            }

            foreach (var client in clients)
                Assert.AreEqual("Hello", await client.ReceiveObservable.ToStrings().Skip(1).Take(1).FirstAsync());

            foreach (var client in clients)
                await client.DisposeAsync();
            await server.DisposeAsync();
        }

        [TestMethod]
        public async Task T40_ClientDisconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
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

            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger, timeout: 10);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Logger.LogInformation(message);
            });

            client.Send("Hello!".ToByteArray());

            await semaphore.WaitAsync();
            if (acceptClient == null)
                throw new NullReferenceException(nameof(acceptClient));

            await server.DisposeAsync();
            await client.DisposeAsync();

            // should throw!
            //acceptClient.Send("Anybody there?".ToByteArray());
            //client.Send("Anybody there?".ToByteArray());
        }

        [TestMethod]
        public async Task T41_ServerDisconnect()
        {
            var semaphore = new SemaphoreSlim(0, 1);

            var endPoint = Utilities.GetEndPointOnRandomLoopbackPort();

            var server = endPoint.CreateRxSocketServer(logger: SocketServerLogger);
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

            var client = await endPoint.ConnectRxSocketClientAsync(logger: SocketClientLogger, timeout: 10);
            client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
            {
                Logger.LogInformation(message);
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

