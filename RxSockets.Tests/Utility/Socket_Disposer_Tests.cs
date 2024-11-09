using System.Threading;
using System.Threading.Tasks;
namespace RxSockets.Tests;

public class Socket_Disposer_Tests(ITestOutputHelper output) : TestBase(output)
{
    private readonly CancellationTokenSource Cts = new();

    [Fact]
    public async Task T01_Dispose_Not_Connected_Socket()
    {
        Socket socket = Utilities.CreateSocket();
        SocketDisposer disposer = new(socket, "?", Cts, Logger);
        await disposer.DisposeAsync();
        Assert.True(disposer.DisposeRequested);
        Assert.False(socket.Connected);
    }

    [Fact]
    public async Task T02_Dispose_Connected_Socket()
    {
        EndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        Socket serverSocket = Utilities.CreateSocket();
        SocketDisposer serverDisposer = new(serverSocket, "?", Cts, Logger);
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        Socket clientSocket = Utilities.CreateSocket();
        SocketDisposer clientDisposer = new(clientSocket, "?", Cts, Logger);
        await clientSocket.ConnectAsync(endPoint);
        Assert.False(clientDisposer.DisposeRequested);
        Assert.True(clientSocket.Connected);

        await clientDisposer.DisposeAsync();

        Assert.True(clientDisposer.DisposeRequested);
        Assert.False(clientSocket.Connected);

        Assert.False(serverDisposer.DisposeRequested);
        Assert.False(serverSocket.Connected);

        await serverDisposer.DisposeAsync();

        Assert.True(serverDisposer.DisposeRequested);
        Assert.False(serverSocket.Connected);
    }

    [Fact]
    public async Task T04_Dispose_Multi()
    {
        EndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        Socket serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        Socket socket = Utilities.CreateSocket();
        SocketDisposer disposer = new(socket, "?", Cts, Logger);
        await socket.ConnectAsync(endPoint);
        Assert.True(socket.Connected);
        Assert.False(disposer.DisposeRequested);

        System.Collections.Generic.List<Task> disposeTasks = Enumerable.Range(1, 8).Select((_) => disposer.DisposeAsync().AsTask()).ToList();
        await Task.WhenAll(disposeTasks);
        Assert.True(disposer.DisposeRequested);
    }

    [Fact]
    public async Task T05_Dispose_Disposed_Socket()
    {
        EndPoint endPoint = TestUtilities.GetEndPointOnRandomLoopbackPort();
        Socket serverSocket = Utilities.CreateSocket();
        serverSocket.Bind(endPoint);
        serverSocket.Listen(10);

        Socket clientSocket = Utilities.CreateSocket();
        SocketDisposer disposer = new(clientSocket, "?", Cts, Logger);
        Assert.False(disposer.DisposeRequested);

        await clientSocket.ConnectAsync(endPoint);
        Assert.True(clientSocket.Connected);

        clientSocket.Dispose();
        await disposer.DisposeAsync(); // dispose disposed
        Assert.True(disposer.DisposeRequested);
    }
}
