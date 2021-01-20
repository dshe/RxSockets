## RxSockets&nbsp;&nbsp; [![Build status](https://ci.appveyor.com/api/projects/status/rfxxbpx2agq8r93n?svg=true)](https://ci.appveyor.com/project/dshe/RxSockets) [![NuGet](https://img.shields.io/nuget/vpre/RxSockets.svg)](https://www.nuget.org/packages/RxSockets/) [![License](https://img.shields.io/badge/license-Apache%202.0-7755BB.svg)](https://opensource.org/licenses/Apache-2.0)
**Minimal Reactive Socket Implementation**
- **asynchronous** connect and disconnect
- **synchronous** send
- **observable** accept and receive
- supports **.NET Standard 2.0**
- dependencies: Reactive Extensions
- simple and intuitive API
- fast

### installation
```csharp
PM> Install-Package RxSockets
```
### example
```csharp
using System;
using System.Net;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Xunit;
using RxSockets;
```
#### server
```csharp
interface IRxSocketServer
{
    IPEndPoint IPEndPoint { get; }
    IObservable<IRxSocketClient> AcceptObservable { get; }
    Task DisposeAsync();
}
```
```csharp
// Create a server using a random available port on the local machine.
IRxSocketServer server = RxSocketServer.Create();

// Find the IPEndPoint of the server.
IPEndPoint ipEndPoint = server.IPEndPoint;

// Start accepting connections from clients.
server.AcceptObservable.Subscribe(onNext: acceptClient =>
{
    // After the server accepts a client connection,
    // start receiving messages from the client and ...
    acceptClient.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
    {
        // echo each message received back to the client.
        acceptClient.Send(message.ToBuffer());
    });
});
```
#### client
```csharp
interface IRxSocketClient
{
    bool Connected { get; }
    void Send(byte[] buffer);
    void Send(byte[] buffer, int offset, int length);
    IAsyncEnumerable<byte> ReadAsync();
    IObservable<byte> ReceiveObservable { get; }
    Task DisposeAsync();
}
```
```csharp
// Create a client by connecting to the server at ipEndPoint.
IRxSocketClient client = await ipEndPoint.ConnectRxSocketClientAsync();

// Start receiving messages from the server.
client.ReceiveObservable.ToStrings().Subscribe(onNext: message =>
{
    // The message received from the server is "Hello!".
    Assert.Equal("Hello!", message);
});

// Send the message "Hello" to the server,
// which the server will then echo back to the client.
client.Send("Hello!".ToBuffer());
```

```csharp
// Allow time for communication to complete.
await Task.Delay(10);

// Disconnect.
await client.DisposeAsync();
await server.DisposeAsync();
```
### notes
```IRxSocketClient.ReadAsync()``` may be used to perform handshaking before subscribing to ```ReceiveObservable```.

```Observable.Publish().AutoConnect()``` may be used to support multiple simultaneous observers.

To communicate using strings (example above), the following extension methods are provided:
```csharp
byte[] ToBuffer(this string source);
Task<string> ToStringAsync(this IAsyncEnumerable<byte> source);
IEnumerable<string> ToStrings(this IEnumerable<byte> source);
IObservable<string> ToStrings(this IObservable<byte> source);
```


