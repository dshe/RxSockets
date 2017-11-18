using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RxSocket.Tests
{
    public class StringMessageTests
    {
        [Fact]
        public void EncodeTest1()
        {
            Assert.Throws<ArgumentNullException>(() => StringMessageEx.EncodeToBytes(null));
        }

        [Theory]
        [InlineData(new byte[] { 0, 0, 0, 0 }, new string[] { })]
        [InlineData(new byte[] { 0, 0, 0, 1, 0 }, new[] { "" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 0, 0 }, new[] { "\0" })]
        [InlineData(new byte[] { 0, 0, 0, 2, 65, 0 }, new[] { "A" })]
        [InlineData(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, new[] { "A", "B" })]
        public void EncodeTest2(byte[] encoded, IEnumerable<string> str)
        {
            Assert.Equal(encoded, StringMessageEx.EncodeToBytes(str));
        }

        [Fact]
        public async Task DecodeTest1()
        {
            Assert.Throws<ArgumentNullException>(() => StringMessageEx.DecodeToStrings(null).ToList());
            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await (new byte[] { 0, 0, 0, 0, 0 }).ToObservable().DecodeToStrings().FirstAsync());
        }

        [Theory]
        [InlineData(new[] { "" }, new byte[] { 0, 0, 0, 1, 0 })]
        [InlineData(new[] { "A" }, new byte[] { 0, 0, 0, 2, 65, 0 })]
        [InlineData(new[] { "A", "B" }, new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 })]
        public async Task DecodeTest2(string[] strings, byte[] bytes)
        {
            Assert.Equal(strings, await bytes.ToObservable().DecodeToStrings().FirstAsync());
        }

    }
}
