using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using RxSockets;
using System.IO;

namespace RxSockets.Tests
{
    public class ConversionsFromByteArrayOfLengthPrefixTest
    {
        [Fact]
        public async Task T01()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await (new byte[] { 0, 0, 0, 0, 0 }).ToObservable().RemoveLengthPrefix().FirstOrDefaultAsync());
        }

        [Theory]
        [InlineData(new byte[] { 0 }, new byte[] { 0, 0, 0, 1, 0 })]
        [InlineData(new byte[] { 65, 0 }, new byte[] { 0, 0, 0, 2, 65, 0 })]
        [InlineData(new byte[] { 65, 0, 66, 0 }, new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 })]
        public async Task T02(byte[] result, byte[] bytes)
        {
            Assert.Equal(result, bytes.RemoveLengthPrefix().SingleOrDefault());
            Assert.Equal(result, await bytes.ToObservable().RemoveLengthPrefix());
        }


        [Fact]
        public void T03()
        {
            Assert.Throws<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { }));
            Assert.Throws<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { 65 }));
            Assert.Throws<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { 65, 0, 65 }));
        }

        [Theory]
        [InlineData(new byte[] { 0 }, new[] { "" })]
        [InlineData(new byte[] { 0, 0 }, new[] { "", "" })]
        [InlineData(new byte[] { 65, 0 }, new[] { "A" })]
        [InlineData(new byte[] { 65, 0, 65, 0, 0 }, new[] { "A", "A", "" })]
        public void T04(byte[] bytes, string[] strings)
        {
            Assert.Equal(strings, ConversionsWithLengthPrefixEx.GetStringArray(bytes));
        }
    }
}
