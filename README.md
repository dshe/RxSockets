## RxSocket&nbsp;&nbsp; [![Build status](https://ci.appveyor.com/api/projects/status/rfxxbpx2agq8r93n?svg=true)](https://ci.appveyor.com/project/dshe/RxSocket) [![NuGet](https://img.shields.io/nuget/vpre/RxSocket.svg)](https://www.nuget.org/packages/RxSocket/) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)
**Minimal Reactive Socket Implementation**
- **asynchronous** connect and disconnect
- **observable** accept and receive
- supports **.NET Standard 2.0**
- dependencies: Reactive Extensions 4
- simple and intuitive API
- tested
- fast

### server
```csharp
interface IRxSocketServer
{
    IObservable<IRxSocketClient> AcceptObservable { get; }
    Task DisconnectAsync();
}
```
```csharp
// Create a socket server on the Endpoint.
IRxSocketServer server = RxSocketServer.Create(IPEndPoint);

// Start accepting connections from clients.
server.AcceptObservable.Subscribe(onNext: acceptClient =>
{
    acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
    {
        // Echo received messages back to the client.
        acceptClient.Send(message.ToByteArray());
    });
});
```
### client
```csharp
interface IRxSocketClient
{
    bool Connected { get; }
    void Send(byte[] buffer, int offset = 0, int length = 0);
    IObservable<byte> ReceiveObservable { get; }
    Task DisconnectAsync();
}
```
```csharp
// Create a socket client by connecting to the server at EndPoint.
(IRxSocketClient? client, Exception? exception) = await RxSocketClient.ConnectAsync(IPEndPoint);
if (client == null)
    throw exception;

client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
{
    // Receive message from the server.
    Assert.Equal("Hello!", message);
});

// Send a message to the server.
client.Send("Hello!".ToByteArray());

// Wait for the message to be received by the server and sent back to the client.
await Task.Delay(100);

await client.DisconnectAsync();
```
