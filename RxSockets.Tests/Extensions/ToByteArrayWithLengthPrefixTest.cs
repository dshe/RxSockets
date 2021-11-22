using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RxSockets.Tests;

public class ToByteArrayWithLengthPrefixTest
{
    [Theory]
    [InlineData(new byte[] { 0, 0, 0, 0 }, new string[] { })]
    [InlineData(new byte[] { 0, 0, 0, 1, 0 }, new[] { "" })]
    [InlineData(new byte[] { 0, 0, 0, 2, 0, 0 }, new[] { "\0" })]
    [InlineData(new byte[] { 0, 0, 0, 2, 65, 0 }, new[] { "A" })]
    [InlineData(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, new[] { "A", "B" })]
    public void T01(byte[] encoded, IEnumerable<string> strings)
    {
        //Assert.Equal(encoded, str.ToByteArrayWithLengthPrefix());
        Assert.Equal(encoded, strings.ToArray().ToByteArray().ToByteArrayWithLengthPrefix());

        //ToByteArrayWithLengthPrefix());
    }
}
