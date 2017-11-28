## RxSocket&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/RxSocket/all.svg)](https://github.com/dshe/RxSocket/releases) [![Build status](https://ci.appveyor.com/api/projects/status/rfxxbpx2agq8r93n?svg=true)](https://ci.appveyor.com/project/dshe/rxsocket) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

**Minimal Reactive Socket Implementation**
- observable receive and accept, asynchronous connect and disconnect, synchronous send
- supports **.NET Standard 2.0**
- simple and intuitive API
- tested
- fast

### server
```csharp
public interface IRxSocketServer
{
    IObservable<IRxSocket> AcceptObservable { get; }
    Task DisconnectAsync(CancellationToken ct);
}
```
```csharp
// Create a socket server at IPEndpoint.
IRxSocketServer server = RxSocketServer.Create(IPEndPoint);

// Wait to accept a client connection.
IRxSocket accept = await server.AcceptObservable.FirstAsync();

// Send a string to the client.
accept.Send("Welcome!".ToBytes());

// Receive a string from the client.
Assert.Equal("Hello", await accept.ReceiveObservable.ToStrings().FirstAsync());

// Disconnect and dispose the sockets.
await Task.WhenAll(accept.DisconnectAsync(), server.DisconnectAsync());

```

### client
```csharp
interface IRxSocket
{
    bool Connected { get; }
    void Send(byte[] buffer, int offset, int length);
    IObservable<byte> ReceiveObservable { get; }
    Task DisconnectAsync(CancellationToken ct);
}
```
```csharp
// Create a socket client by successfully connecting to the server at IPEndPoint.
(SocketError error, IRxSocket client) = await RxSocket.ConnectAsync(IPEndPoint);
Assert.Equal(SocketError.Success, error);

// Receive a string from the server.
Assert.Equal("Welcome!", await client.ReceiveObservable.ToStrings().FirstAsync());

// Send a string to the server.
client.Send("Hello".ToBytes());

// Disconnect and dispose the socket.
await client.DisconnectAsync();
```
