namespace RxSockets.Tests;

public sealed class Socket_Recieve_Tests(ITestOutputHelper output) : TestBase(output), IDisposable
{
    private readonly Socket ServerSocket = Utilities.CreateSocket();
    private readonly Socket Socket = Utilities.CreateSocket();

    public void Dispose()
    {
        ServerSocket.Close();
        Socket.Close();
    }

    [Fact]
    public void T01_Disconnect()
    {   
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, 0);
        ServerSocket.Bind(ipEndPoint);
        ServerSocket.Listen(10);

        EndPoint endPoint = ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint");
        Socket.Connect(endPoint);

        Socket accepted = ServerSocket.Accept();
        accepted.Disconnect(false);

        byte[] buffer = new byte[10];
        int bytes = Socket.Receive(buffer, SocketFlags.None);
        // after the remote socket disconnects, Socket.Receive() returns 0 bytes
        Assert.Equal(0, bytes);
    }

    [Fact]
    public async Task T02_Disconnect_ReceiveBytesAsync()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, 0);
        ServerSocket.Bind(ipEndPoint);
        ServerSocket.Listen(10);

        EndPoint endPoint = ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint");
        await Socket.ConnectAsync(endPoint, TestContext.Current.CancellationToken);

        Socket accepted = await ServerSocket.AcceptAsync(TestContext.Current.CancellationToken);
        await accepted.DisconnectAsync(false, TestContext.Current.CancellationToken);

        SocketReceiver reader = new(Socket, "?", Logger, default);

        // after the remote socket disconnects, reader.ReceiveByteAsync() returns nothing
        bool any = await reader.ReceiveAllAsync(TestContext.Current.CancellationToken).AnyAsync(TestContext.Current.CancellationToken);
        Assert.False(any);
    }

    [Fact]
    public async Task T03_Disconnect_SocketReceiver()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, 0);
        ServerSocket.Bind(ipEndPoint);
        ServerSocket.Listen(10);

        EndPoint endPoint = ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint");
        await Socket.ConnectAsync(endPoint, TestContext.Current.CancellationToken);
        Socket accepted = await ServerSocket.AcceptAsync(TestContext.Current.CancellationToken);

        SocketReceiver reader = new(Socket, "?", Logger, default);
        //var observable = reader.ReceiveObservable;
        System.Collections.Generic.IAsyncEnumerable<byte> xxx = reader.ReceiveAllAsync(TestContext.Current.CancellationToken);

        accepted.Close();

        // after the remote socket disconnects, the observable completes
        //var result = await observable.SingleOrDefaultAsync();
        byte result = await xxx.SingleOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, result); // default
    }

    [Fact]
    public async Task T04_Disconnect_And_Send()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, 0);
        ServerSocket.Bind(ipEndPoint);
        ServerSocket.Listen(10);

        EndPoint endPoint = ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint");
        await Socket.ConnectAsync(endPoint, TestContext.Current.CancellationToken);
        Socket accepted = await ServerSocket.AcceptAsync(TestContext.Current.CancellationToken);
        Assert.True(Socket.Connected);
        Assert.True(accepted.Connected);

        accepted.Close();

        Socket.Send([1]);

        await Task.Yield();

        // after the remote socket disconnects, Send() throws on second usage
        Assert.Throws<SocketException>(() => Socket.Send([1]));
    }

    [Fact]
    public async Task T05_Receive()
    {
        IPEndPoint ipEndPoint = new(IPAddress.Loopback, 0);
        ServerSocket.Bind(ipEndPoint);
        ServerSocket.Listen(10);

        EndPoint endPoint = ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint");
        await Socket.ConnectAsync(endPoint, TestContext.Current.CancellationToken);
        Socket accepted = await ServerSocket.AcceptAsync(TestContext.Current.CancellationToken);
        accepted.Send([1]);

        SocketReceiver reader = new(Socket, "?", Logger, default);
        System.Collections.Generic.IAsyncEnumerable<byte> xxx = reader.ReceiveAllAsync(TestContext.Current.CancellationToken);
        byte result = await xxx.FirstAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, result);
        accepted.Close();
    }
}
