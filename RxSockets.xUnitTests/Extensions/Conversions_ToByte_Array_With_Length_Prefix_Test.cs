using System;
using System.Collections.Generic;
using Xunit;

namespace RxSockets.xUnitTests
{
    public class Conversions_ToByte_Array_With_Length_Prefix_Test
    {
        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0 }, new string[] { })]
        [InlineData(new byte[] { 0, 0, 0, 1, 0 }, new[] { "" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 0, 0 }, new[] { "\0" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 65, 0 }, new[] { "A" })]
        [InlineData(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, new[] { "A", "B" })]
        public void T01(byte[] encoded, IEnumerable<string> str)
        {
            Assert.Equal(encoded, ConversionsWithLengthPrefixEx.ToByteArrayWithLengthPrefix(str));
        }
    }
}
