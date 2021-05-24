using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using RxSockets;
using System.IO;

namespace RxSockets.Tests
{
    public class ToArraysFromBytesWithLengthPrefix
    {
        [Fact]
        public async Task T01()
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await (new byte[] { 0, 0, 0, 0, 0 }).ToObservable().ToArraysFromBytesWithLengthPrefix().FirstAsync());
        }

        [Theory]
        [InlineData(new byte[] { 0 }, new byte[] { 0, 0, 0, 1, 0 })]
        [InlineData(new byte[] { 65, 0 }, new byte[] { 0, 0, 0, 2, 65, 0 })]
        [InlineData(new byte[] { 65, 0, 66, 0 }, new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 })]
        public async Task T02(byte[] result, byte[] bytes)
        {
            Assert.Equal(result, bytes.ToArraysFromBytesWithLengthPrefix().First());
            Assert.Equal(result, await bytes.ToObservable().ToArraysFromBytesWithLengthPrefix());
        }

        [Fact]
        public void T03()
        {
            Assert.Throws<InvalidOperationException>(() => LengthPrefixExtensions.ToArraysFromBytesWithLengthPrefix(new byte[] { }).First());
            Assert.Throws<InvalidDataException>(() => LengthPrefixExtensions.ToArraysFromBytesWithLengthPrefix(new byte[] { 65 }).First());
            Assert.Throws<InvalidDataException>(() => LengthPrefixExtensions.ToArraysFromBytesWithLengthPrefix(new byte[] { 65, 0, 65 }).First());
        }
    }
}
