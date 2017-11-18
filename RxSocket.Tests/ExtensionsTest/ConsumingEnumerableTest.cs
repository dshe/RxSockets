using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using RxSocket.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace RxSocket.Tests
{
    public class ConsumingEnumerableTest
    {
        private readonly Action<string> Write;
        public ConsumingEnumerableTest(ITestOutputHelper output) => Write = output.WriteLine;

        [Fact]
        public async Task NeverTest()
        {
            var enumerator = Observable.Never<int>().ToConsumingEnumerable().GetEnumerator();
            var task = Task.Run(() => enumerator.MoveNext());
            Assert.NotEqual(task, await Task.WhenAny(task, Task.Delay(100)));
        }

        [Fact]
        public void EmptyTest()
        {
            var enumerator = Observable.Empty<int>().ToConsumingEnumerable().GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void ThrowTest()
        {
            var enumerator = Observable.Throw<int>(new DivideByZeroException()).ToConsumingEnumerable().GetEnumerator();
            Assert.Throws<DivideByZeroException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void ReturnTest()
        {
            var enumerator = Observable.Return(1).ToConsumingEnumerable().GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public async Task SendAndReceive()
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, NetworkUtility.GetRandomUnusedPort());

            var server = RxSocketServer.Create(endPoint);

            var acceptTask = server.AcceptObservable.FirstAsync().ToTask();

            (SocketError error, IRxSocket client) = RxSocket.ConnectAsync(endPoint).Result;
            Assert.Equal(SocketError.Success, error);

            var accept = await acceptTask;
            Assert.True(accept.Connected && client.Connected);

            var clientConsumingEnumerable = client.ReceiveObservable.ToStrings().ToConsumingEnumerable();
            var acceptConsumingEnumerable = accept.ReceiveObservable.ToStrings().ToConsumingEnumerable();

            // The server sends a string to the client.
            accept.Send("Welcome!".ToBytes());

            Assert.Equal("Welcome!", clientConsumingEnumerable.Take(1).Single());


            // The client sends a string to the server.
            client.Send("Hello".ToBytes());

            Assert.Equal("Hello", acceptConsumingEnumerable.Take(1).Single());

            accept.Send("A".ToBytes());
            "B".ToBytes().SendTo(accept); // Note SendTo() extension method.
            Assert.Equal(new[] { "A", "B" }, clientConsumingEnumerable.Take(2).ToArray());

            acceptConsumingEnumerable.Dispose();
            clientConsumingEnumerable.Dispose();
            await Task.WhenAll(client.DisconnectAsync(), accept.DisconnectAsync(), server.DisconnectAsync());
        }
    }
}
