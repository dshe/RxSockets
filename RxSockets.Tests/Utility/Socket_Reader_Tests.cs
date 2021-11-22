using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Net;

namespace RxSockets.Tests;

public class Socket_Recieve_Tests : TestBase, IDisposable
{
    private readonly Socket ServerSocket = Utilities.CreateSocket();
    private readonly Socket Socket = Utilities.CreateSocket();
    public Socket_Recieve_Tests(ITestOutputHelper output) : base(output) { }

    public void Dispose()
    {
        ServerSocket.Close();
        Socket.Close();
    }

    [Fact]
    public void T01_Disconnect()
    {
        var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        endPoint = (IPEndPoint)(ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint"));
        Socket.Connect(endPoint);

        var accepted = ServerSocket.Accept();
        accepted.Disconnect(false);

        byte[] buffer = new byte[10];
        int bytes = Socket.Receive(buffer, SocketFlags.None);
        // after the remote socket disconnects, Socket.Receive() returns 0 bytes
        Assert.True(bytes == 0);
    }

    [Fact]
    public async Task T02_Disconnect_ReceiveBytesAsync()
    {
        var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        endPoint = (IPEndPoint)(ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint"));
        Socket.Connect(endPoint);

        var accepted = ServerSocket.Accept();
        accepted.Disconnect(false);

        var reader = new SocketReceiver(Socket, Logger, "?");

        // after the remote socket disconnects, reader.ReceiveByteAsync() returns nothing
        var any = await reader.ReceiveAllAsync(default).AnyAsync();
        Assert.False(any);
    }

    [Fact]
    public async Task T03_Disconnect_SocketReceiver()
    {
        var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        endPoint = (IPEndPoint)(ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint"));
        Socket.Connect(endPoint);
        var accepted = ServerSocket.Accept();

        var reader = new SocketReceiver(Socket, Logger, "?");
        //var observable = reader.ReceiveObservable;
        var xxx = reader.ReceiveAllAsync(default);

        accepted.Close();

        // after the remote socket disconnects, the observable completes
        //var result = await observable.SingleOrDefaultAsync();
        var result = await xxx.SingleOrDefaultAsync();

        Assert.Equal(0, result); // default
    }

    [Fact]
    public async Task T04_Disconnect_And_Send()
    {
        var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        endPoint = (IPEndPoint)(ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint"));
        Socket.Connect(endPoint);
        var accepted = ServerSocket.Accept();
        Assert.True(Socket.Connected);
        Assert.True(accepted.Connected);

        accepted.Close();

        Socket.Send(new byte[1] { 1 });

        await Task.Yield();

        // after the remote socket disconnects, Send() throws on second usage
        Assert.Throws<SocketException>(() => Socket.Send(new byte[1] { 1 }));
    }

    [Fact]
    public async Task T05_Receive()
    {
        var endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 0);
        ServerSocket.Bind(endPoint);
        ServerSocket.Listen(10);
        endPoint = (IPEndPoint)(ServerSocket.LocalEndPoint ?? throw new InvalidOperationException("EndPoint"));
        Socket.Connect(endPoint);
        var accepted = ServerSocket.Accept();
        accepted.Send(new byte[] { 1 });

        var reader = new SocketReceiver(Socket, Logger, "?");
        var xxx = reader.ReceiveAllAsync(default);
        var result = await xxx.FirstAsync();
        Assert.Equal(1, result);
        accepted.Close();
    }

}
