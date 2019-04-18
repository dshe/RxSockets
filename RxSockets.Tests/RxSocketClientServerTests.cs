using Xunit;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Threading;

#nullable enable

namespace RxSockets.Tests
{
    public class RxSocketClientServerTest : TestBase, IDisposable
    {
        private readonly IRxSocketServer Server;
        private readonly Task<IRxSocketClient> AcceptTask;

        public RxSocketClientServerTest(ITestOutputHelper output) : base(output)
        {
            Server = RxSocketServer.Create(IPEndPoint, SocketServerLogger);
            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
        }

        public void Dispose()
        {
            Server.Dispose();
        }

        [Fact]
        public async Task T01_Disc()
        {
            var client = await RxSocketClient.ConnectAsync(IPEndPoint);
            var accept = await AcceptTask;

            int i = 0;

            var cts = new CancellationTokenSource();
            NewThreadScheduler.Default.Schedule(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    i++;
                    new[] { $"v{i}..{i}" }.ToByteArrayWithLengthPrefix().SendTo(accept);
                    await Task.Delay(1000);
                }

            });

            var x1 = await client.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().Take(2).ToList();
            await Task.Delay(500);
            var x2 = await client.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().Take(2).ToList();

            ;




            client.Dispose();
        }

    }
}
