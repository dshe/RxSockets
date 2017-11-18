## RxSocket&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/XSocket/all.svg)](https://github.com/dshe/XSocket/releases) [![Build status](https://ci.appveyor.com/api/projects/status/7pafhsjfxcc36yfa?svg=true)](https://ci.appveyor.com/project/dshe/xsocket) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

***Minimal Reactive Socket Implementation***
- observable receive and accept, asynchronous connect and disconnect, synchronous send
- supports **.NET Standard 2.0**
- simple and intuitive API
- tested
- fast

#### client
```csharp
interface IRxSocket
{
    bool Connected { get; }
    void Send(byte[] buffer, int offset, int length);
    IObservable<byte> ReceiveObservable { get; }
    Task DisconnectAsync(int timeout);
}
```
```csharp
// Create a socket client by successfully connecting to the server at EndPoint.
(SocketError error, IRxSocket client) = await RxSocket.ConnectAsync(EndPoint);
Assert.Equal(SocketError.Success, error);

// Receive a string from the server.
Assert.Equal("Welcome!", await client.ReceiveObservable.ToStrings().FirstAsync());

// Send a string to to the server.
client.Send("Hello".ToBytes());

// Disconnect and dispose.
await client.DisconnectAsync();
```

#### server
```csharp
public interface IRxSocketServer
{
    IObservable<IRxSocket> AcceptObservable { get; }
    Task DisconnectAsync(int timeout);
}
```
```csharp
// Create a socket server at Endpoint.
var server = RxSocketServer.Create(EndPoint);

// Wait to accept a client connection.
IRxSocket accept = await server.AcceptObservable.FirstAsync();

// Send a string to the client.
accept.Send("Welcome!".ToBytes());

// Receive a string from the client.
Assert.Equal("Hello", await accept.ReceiveObservable.ToStrings().FirstAsync();

// Disconnect and dispose.
await Task.WhenAll(accept.DisconnectAsync(), server.DisconnectAsync());

```
