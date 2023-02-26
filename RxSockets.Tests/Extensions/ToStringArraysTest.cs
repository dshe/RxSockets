using System.IO;
using System.Text;

namespace RxSockets.Tests;

public class ToStringArraysTest
{
    private readonly MemoryStream ms = new();

    private void AddMessage(string str)
    {
        long start = ms.Position;
        ms.Position += 4;
        Encoding.UTF8.GetBytes(str).ToList().ForEach(ms.WriteByte);
        ms.WriteByte(0);
        int len = Convert.ToInt32(ms.Position - start - 4);
        int prefix = IPAddress.NetworkToHostOrder(len);
        long lastPos = ms.Position;
        ms.Position = start;
        BitConverter.GetBytes(prefix).ToList().ForEach(ms.WriteByte);
        ms.Position = lastPos;
    }

    [Fact]
    public void T01_Test_String()
    {
        AddMessage("A\0BC\0");
        string[][] messages = ms.ToArray().ToArraysFromBytesWithLengthPrefix().ToStringArrays().ToArray();
        Assert.Single(messages); // 1 message
        string[] message1 = messages[0];
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
        byte[] array = ms.ToArray();

        string[][] messages = array.ToArraysFromBytesWithLengthPrefix().ToStringArrays().ToArray();
        Assert.Equal(3, messages.Length); // 3 messages
        string[] message1 = messages[0]; // message 1
        Assert.Equal(3, message1.Length); // contains 3 strings
        Assert.Equal("A", message1[0]);
        Assert.Equal("BC", message1[1]);
        Assert.Equal("", message1[2]);

        string[] message2 = messages[1];
        Assert.Single(message2);
        Assert.Equal("D", message2[0]);

        string[] message3 = messages[2];
        Assert.Single(message3);
        Assert.Equal("", message3[0]);
    }

}

