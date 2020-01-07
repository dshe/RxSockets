## RxSocket&nbsp;&nbsp; [![Build status](https://ci.appveyor.com/api/projects/status/rfxxbpx2agq8r93n?svg=true)](https://ci.appveyor.com/project/dshe/RxSocket) [![NuGet](https://img.shields.io/nuget/vpre/RxSockets.svg)](https://www.nuget.org/packages/RxSockets/) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)
**Minimal Reactive Socket Implementation**
- **observable** receive and accept
- **asynchronous** connect and dispose
- supports **.NET Standard 2.0**
- dependencies: Reactive Extensions
- simple and intuitive API
- tested
- fast

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using RxSockets;
```

### server
```csharp
interface IRxSocketServer
{
    IObservable<IRxSocketClient> AcceptObservable { get; }
    Task DisposeAsync();
}
```
```csharp
// Create an IPEndPoint on the local machine on an available arbitrary port.
IPEndPoint endPoint = new IPEndPoint(IPAddress.IPv6Loopback, 12345);

// Create a socket server on the IPEndPoint.
IRxSocketServer server = endPoint.CreateRxSocketServer();

// Start accepting connections from clients.
server.AcceptObservable.Subscribe(onNext: acceptClient =>
{
    // After the server accepts a client connection, start receiving messages from the client and ...
    acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
    {
        // Echo each message received back to the client.
        acceptClient.Send(message.ToByteArray());
    });
});
```
### client
```csharp
interface IRxSocketClient
{
    bool Connected { get; }
    void Send(byte[] buffer);
    void Send(byte[] buffer, int offset, int length);
    Task<byte> ReadAsync();
    IObservable<byte> ReceiveObservable { get; }
    Task DisposeAsync();
}
```
```csharp
// Create a socket client by first connecting to the server at the IPEndPoint.
IRxSocketClient client = await endPoint.ConnectRxSocketClientAsync();

// Start receiving messages from the server.
client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
{
    // The message received from the server is "Hello!".
    Assert.Equal("Hello!", message);
});

// Send the message "Hello" to the server (which will then be echoed back to the client).
client.Send("Hello!".ToByteArray());

await client.DisposeAsync();
await server.DisposeAsync();
```
