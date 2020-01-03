using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Xunit;



namespace RxSockets.Tests
{
    public class LengthPrefixTest
    {
        private readonly MemoryStream ms = new MemoryStream();

        private void AddMessage(string str)
        {
            var start = ms.Position;
            ms.Position += 4;
            Encoding.UTF8.GetBytes(str).ToList().ForEach(ms.WriteByte);
            ms.WriteByte(0);
            var len = Convert.ToInt32(ms.Position - start - 4);
            var prefix = IPAddress.NetworkToHostOrder(len);
            var lastPos = ms.Position;
            ms.Position = start;
            BitConverter.GetBytes(prefix).ToList().ForEach(ms.WriteByte);
            ms.Position = lastPos;
        }

        [Fact]
        public void T01_Test_String()
        {
            AddMessage("A\0BC\0");
            var array = ms.ToArray();
            var messages = array.FromByteArrayWithLengthPrefix().ToStringArray().ToArray();
            Assert.Single(messages); // 1 message
            var message1 = messages[0];
            Assert.Equal(3, message1.Length); // containing 3 strings
            Assert.Equal("A", message1[0]);
            Assert.Equal("BC", message1[1]);
            Assert.Equal("", message1[2]);
        }

        [Fact]
        public void T02_Test_Message()
        {
            AddMessage("A\0BC\0");
            AddMessage("D");
            AddMessage("");
            var array = ms.ToArray();

            var messages = array.FromByteArrayWithLengthPrefix().ToStringArray().ToArray();
            Assert.Equal(3, messages.Length); // 3 messages
            var message1 = messages[0]; // message 1
            Assert.Equal(3, message1.Length); // contains 3 strings
            Assert.Equal("A", message1[0]);
            Assert.Equal("BC", message1[1]);
            Assert.Equal("", message1[2]);

            var message2 = messages[1];
            Assert.Single(message2);
            Assert.Equal("D", message2[0]);

            var message3 = messages[2];
            Assert.Single(message3);
            Assert.Equal("", message3[0]);
        }

    }
}

