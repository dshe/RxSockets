## RxSocket&nbsp;&nbsp; [![release](https://img.shields.io/github/release/dshe/RxSocket/all.svg)](https://github.com/dshe/RxSocket/releases) [![Build status](https://ci.appveyor.com/api/projects/status/rfxxbpx2agq8r93n?svg=true)](https://ci.appveyor.com/project/dshe/rxsocket) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)

**Minimal Reactive Socket Implementation**
- observable receive and accept, asynchronous connect and disconnect
- supports **.NET Standard 2.0**
- simple and intuitive API
- tested
- fast

### server
```csharp
interface IRxSocketServer
{
    IObservable<IRxSocket> AcceptObservable { get; }
    Task DisconnectAsync(CancellationToken ct = default);
}
```
```csharp
// Create a socket server on the Endpoint.
IRxSocketServer server = RxSocketServer.Create(IPEndPoint);

// Start accepting connections from clients.
server.AcceptObservable.Subscribe(acceptClient =>
{
    acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
    {
        // Echo message received back to the client.
        acceptClient.Send(message.ToByteArray());
    });
});
```
### client
```csharp
interface IRxSocket
{
    bool Connected { get; }
    void Send(byte[] buffer, int offset = 0, int length = 0);
    IObservable<byte> ReceiveObservable { get; }
    Task DisconnectAsync(CancellationToken ct = default);
}
```
```csharp
// Create a socket client by connecting to the server at EndPoint.
IRxSocket client = await RxSocket.ConnectAsync(IPEndPoint);

client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
{
    // Receive message from the server.
    Assert.Equal("Hello!", message);
});

// Send a message to the server.
client.Send("Hello!".ToByteArray());

// Wait for the message to be received from the server.
await Task.Delay(100);

await client.DisconnectAsync();
```
