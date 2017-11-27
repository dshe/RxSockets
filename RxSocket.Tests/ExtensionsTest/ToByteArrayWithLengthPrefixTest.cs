using System;
using System.Collections.Generic;
using Xunit;

namespace RxSocket.Tests
{
    public class ToByteArrayWithLengthPrefixTest
    {
        [Fact]
        public void T01()
        {
            Assert.Throws<ArgumentNullException>(() => ConversionsWithLengthPrefixEx.ToByteArrayWithLengthPrefix(null));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0 }, new string[] { })]
        [InlineData(new byte[] { 0, 0, 0, 1, 0 }, new[] { "" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 0, 0 }, new[] { "\0" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 65, 0 }, new[] { "A" })]
        [InlineData(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, new[] { "A", "B" })]
        public void T02(byte[] encoded, IEnumerable<string> str)
        {
            Assert.Equal(encoded, ConversionsWithLengthPrefixEx.ToByteArrayWithLengthPrefix(str));
        }
    }
}
