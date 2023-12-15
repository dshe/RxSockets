using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets.Tests;

public class Examples : TestBase
{
    public Examples(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task T00_Example()
    {
        // Create a socket server on an available port on the local host.
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        // Start accepting connections from clients.
        server.AcceptAllAsync.ToObservableFromAsyncEnumerable().Subscribe(acceptClient =>
        {
            // After the server accepts a client connection...
            acceptClient.ReceiveAllAsync.ToStrings().ToObservable().Subscribe(onNext: message =>
            {
                // Echo each message received back to the client.
                acceptClient.Send(message.ToByteArray());
            });
        });

        // Create a socket client by first connecting to the server at the EndPoint.
        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);

        // Start receiving messages from the server.
        client.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().Subscribe(onNext: message =>
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
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        // Start a task to allow the server to accept the next client connection.
        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync();

        // Create a socket client by successfully connecting to the server at EndPoint.
        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);

        // Get the client socket accepted by the server.
        IRxSocketClient accept = await acceptTask;
        Assert.True(accept.Connected && client.Connected);

        // start a task to receive the first string from the server.
        Task<string> dataTask = client.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().FirstAsync().ToTask();

        // The server sends a string to the client.
        accept.Send("Welcome!".ToByteArray());
        Assert.Equal("Welcome!", await dataTask);

        await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T10_Receive_Observable()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        ValueTask<IRxSocketClient> acceptTask = server.AcceptAllAsync.FirstAsync();
        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        IRxSocketClient accept = await acceptTask;

        IDisposable sub = client.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().Subscribe(str =>
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
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        server.AcceptAllAsync.ToObservableFromAsyncEnumerable()
            .Subscribe(accepted => accepted.Send("Welcome!".ToByteArray()));

        IRxSocketClient client1 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        IRxSocketClient client2 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        IRxSocketClient client3 = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);

        Assert.Equal("Welcome!", await client1.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());
        Assert.Equal("Welcome!", await client2.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());
        Assert.Equal("Welcome!", await client3.ReceiveAllAsync.ToStrings().ToObservableFromAsyncEnumerable().Take(1).FirstAsync());

        await client1.DisposeAsync();
        await client2.DisposeAsync();
        await client3.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T30_Both()
    {
        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        server.AcceptAllAsync.ToObservableFromAsyncEnumerable().Subscribe(accepted =>
        {
            accepted.Send("Welcome!".ToByteArray());
            accepted
                .ReceiveAllAsync
                .ToObservableFromAsyncEnumerable()
                .ToStrings()
                .Subscribe(s => accepted.Send(s.ToByteArray()));
        });


        List<IRxSocketClient> clients = new();
        for (int i = 0; i < 3; i++)
        {
            IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
            client.Send("Hello".ToByteArray());
            clients.Add(client);
        }

        foreach (IRxSocketClient client in clients)
            Assert.Equal("Hello", await client.ReceiveAllAsync.ToObservableFromAsyncEnumerable().ToStrings().Skip(1).Take(1).FirstAsync());

        foreach (IRxSocketClient client in clients)
            await client.DisposeAsync();
        await server.DisposeAsync();
    }

    [Fact]
    public async Task T40_Client_Disconnect()
    {
        SemaphoreSlim semaphore = new(0, 1);

        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        IRxSocketClient? acceptClient = null;
        server.AcceptAllAsync.ToObservableFromAsyncEnumerable().Subscribe(ac =>
        {
            acceptClient = ac;
            semaphore.Release();
            acceptClient.ReceiveAllAsync.ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
            {
                acceptClient.Send(message.ToByteArray());
            });
        });

        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        client.ReceiveAllAsync.ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
        {
            Write(message);
        });

        client.Send("Hello!".ToByteArray());

        await semaphore.WaitAsync();
        if (acceptClient is null)
            throw new NullReferenceException(nameof(acceptClient));

        await server.DisposeAsync();
        await client.DisposeAsync();

        semaphore.Dispose();
    }

    [Fact]
    public async Task T41_Server_Disconnect()
    {
        SemaphoreSlim semaphore = new(0, 1);

        IRxSocketServer server = RxSocketServer.Create(SocketServerLogger);
        EndPoint endPoint = server.LocalEndPoint;

        IRxSocketClient? acceptClient = null;
        server.AcceptAllAsync.ToObservableFromAsyncEnumerable().Subscribe(ac =>
        {
            acceptClient = ac;
            semaphore.Release();
            acceptClient.ReceiveAllAsync.ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
            {
                acceptClient.Send(message.ToByteArray());
            });
        });

        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync(SocketClientLogger);
        client.ReceiveAllAsync.ToObservableFromAsyncEnumerable().ToStrings().Subscribe(onNext: message =>
        {
            Write(message);
        });

        client.Send("Hello!".ToByteArray());
        await semaphore.WaitAsync();
        if (acceptClient is null)
            throw new NullReferenceException(nameof(acceptClient));

        await server.DisposeAsync();
        await client.DisposeAsync();
    }
}
