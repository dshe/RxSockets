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
    Task DisconnectAsync(CancellationToken ct);
}
```
```csharp
// Create a socket server on the endpoint.
var server = RxSocketServer.Create(IPEndPoint);

// Start accepting connections from clients.
server.AcceptObservable.Subscribe(acceptClient =>
{
    acceptClient.Send("Welcome!".ToByteArray());

    CancellationTokenSource cts = null;

    acceptClient.ReceiveObservable.ToStrings().Subscribe(
    onNext: message =>
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            while (true)
            {
                if (message == "USD/EUR")
                    acceptClient.Send("1.30".ToByteArray());
                else if (message == "JPY/USD")
                    acceptClient.Send("110".ToByteArray());
                await Task.Delay(100, cts.Token);
            }
        });
    },
    onCompleted: () => cts?.Cancel());
});

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
// Create a socket client by connecting to the server at EndPoint.
var client = await RxSocket.TryConnectAsync(IPEndPoint);

Assert.Equal("Welcome!", await client.ReceiveObservable.ToStrings().FirstAsync());

client.Send("USD/EUR".ToByteArray());

client.ReceiveObservable.ToStrings().Subscribe(message =>
{
    // Receive exchange rates stream from server.
    Assert.Equal("1.30", message);
});

await Task.Delay(1000);

await client.DisconnectAsync();
```
