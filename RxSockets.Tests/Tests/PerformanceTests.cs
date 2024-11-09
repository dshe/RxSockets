using System.Diagnostics;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
namespace RxSockets.Tests;

public class PerformanceTest1(ITestOutputHelper output) : TestBase(output)
{
    const int numberOfMessages = 100_000;

    [Fact]
    public async Task T01_ReceiveStrings()
    {
        IRxSocketServer server = RxSocketServer.Create();
        EndPoint endPoint = server.LocalEndPoint;

        ValueTask<IRxSocketClient> acceptFirstClientTask = server.AcceptAllAsync.FirstAsync();
        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync();
        IRxSocketClient acceptClient = await acceptFirstClientTask;
        Task<int> countTask = acceptClient.ReceiveObservable.ToStrings().Count().ToTask();
        //ValueTask<int> countTask = acceptClient.ReceiveAllAsync.ToStrings().CountAsync();

        byte[] message = "Welcome!".ToByteArray();
        client.Send(message);

        Stopwatch watch = new();
        watch.Start();

        // send messages from server to client
        for (int i = 0; i < numberOfMessages - 1; i++)
            client.Send(message);

        // end countTask
        await client.DisposeAsync();
        int count = await countTask;
        watch.Stop();

        Assert.Equal(numberOfMessages, count);

        long frequency = Stopwatch.Frequency * numberOfMessages / watch.ElapsedTicks;
        Write($"{frequency:N0} messages / second");

        await server.DisposeAsync();
    }
}

public class PerformanceTest2(ITestOutputHelper output) : TestBase(output)
{
    const int numberOfMessages = 100_000;

    [Fact]
    public async Task T02_ReceiveStringsFromPrefixedBytes()
    {
        IRxSocketServer server = RxSocketServer.Create();
        EndPoint endPoint = server.LocalEndPoint;
        ValueTask<IRxSocketClient> acceptFirstClientTask = server.AcceptAllAsync.FirstAsync();

        IRxSocketClient client = await endPoint.CreateRxSocketClientAsync();
        Assert.True(client.Connected);

        Task<int> countTask = client
            .ReceiveObservable
            .ToArraysFromBytesWithLengthPrefix()
            .ToStringArrays()
            .Count()
            .ToTask();
        /*
        ValueTask<int> countTask = client
            .ReceiveAllAsync
            .ToArraysFromBytesWithLengthPrefix()
            .ToStringArrays()
            .CountAsync();
        */

        IRxSocketClient acceptClient = await acceptFirstClientTask;
        Assert.True(acceptClient.Connected);

        byte[] message = new[] { "Welcome!" }.ToByteArray().ToByteArrayWithLengthPrefix();

        acceptClient.Send("1".ToByteArray().ToByteArrayWithLengthPrefix());

        Stopwatch watch = new();
        watch.Start();

        for (int i = 0; i < numberOfMessages - 1; i++)
            acceptClient.Send(message);

        // end count task
        await acceptClient.DisposeAsync();
        int count = await countTask;

        watch.Stop();
        Assert.Equal(numberOfMessages, count);

        long frequency = Stopwatch.Frequency * numberOfMessages / watch.ElapsedTicks;
        Write($"{frequency:N0} messages / second");

        await client.DisposeAsync();
        await server.DisposeAsync();
    }
}
