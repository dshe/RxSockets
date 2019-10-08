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
using System.IO;
using System.Linq;

#nullable enable

namespace RxSockets.Tests
{
    public class RxSocketClientServerTest : TestBase, IDisposable
    {
        private readonly IRxSocketServer Server;
        private readonly Task<IRxSocketClient> AcceptTask;

        public RxSocketClientServerTest(ITestOutputHelper output) : base(output)
        {
            Server = IPEndPoint.CreateRxSocketServer(SocketServerLogger);

            AcceptTask = Server.AcceptObservable.FirstAsync().ToTask();
        }

        public void Dispose() => Server.Dispose();

        [Fact]
        public async Task T01_Disc()
        {
            var client = await IPEndPoint.ConnectRxSocketClientAsync();

            var accept = await AcceptTask;


            //var firstString = await accept.ReceiveObservable.ObserveOn(NewThreadScheduler.Default);

            NewThreadScheduler.Default.Schedule(async () =>
            {
                var firstString = await accept.ReceiveObservable.ToStrings().FirstAsync();
                if (firstString != "A")
                    throw new InvalidDataException("'A' not received.");

                /*
                accept.ReceiveObservable.Subscribe(x =>
                {
                    Write(x.ToString());
                });
                */

                //await Task.Delay(100);

                //var messages0 = await accept.ReceiveObservable.FirstAsync();
                //var messages1 = await accept.ReceiveObservable.FirstAsync();

                //var messages1 = await accept.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().FirstAsync();
                //var messages2 = await accept.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().FirstAsync();

                ;
                //var messages = await accept.ReceiveObservable.ToByteArrayOfLengthPrefix().ToStringArray().Take(2).ToList();
                //var versions = messages[0].Single();
                
                //if (messages[1][0] != "71") // receive StartApi message
                //    throw new InvalidDataException("StartApi message not received.");
                
                //new[] { "149", DateTime.Now.ToString("yyyyMMdd HH:mm:ss XXX")}.ToByteArrayWithLengthPrefix().SendTo(accept);
                //new[] { "15", "1", "123,456,789" }.ToByteArrayWithLengthPrefix().SendTo(accept);
                //new[] { "9", "1", "10" }.ToByteArrayWithLengthPrefix().SendTo(accept);

            });

            "A".ToByteArray().SendTo(client);

            // Start sending and receiving messages with an int32 message length prefix (UseV100Plus).
            new[] { $"B" }
            .ToByteArrayWithLengthPrefix().SendTo(client);

            new[] { "C", "D" }
            .ToByteArrayWithLengthPrefix().SendTo(client);

            await Task.Delay(3000);
            ;

            Write("complete");


            client.Dispose();
        }

    }
}
