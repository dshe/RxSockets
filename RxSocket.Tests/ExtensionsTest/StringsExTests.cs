using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RxSocket.Tests
{
    public class StringsExTest
    {
        [Fact]
        public void ToBytesTest()
        {
            Assert.Throws<ArgumentNullException>(() => ((string)null).ToBytes());
            Assert.Equal(new byte[] { 0 }, "".ToBytes());
            Assert.Equal(new byte[] { 65, 0 }, "A".ToBytes());
            Assert.Equal(new byte[] { 65, 0, 0 }, "A\0".ToBytes());
        }

        [Fact]
        public async Task ToStringsTest()
        {
            Assert.Throws<ArgumentNullException>(() => StringsEx.ToStrings(null));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await new byte[] { }.ToObservable().ToStrings().FirstAsync());

            Assert.Equal("", await new byte[] { 0 }.ToObservable().ToStrings().FirstAsync());

            await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await new byte[] { 65 }.ToObservable().ToStrings().FirstAsync()); // not terminated

            Assert.Equal("A", await new byte[] { 65, 0 }.ToObservable().ToStrings().FirstAsync());

            Assert.Equal("B", await new byte[] { 65, 0, 66, 0 }.ToObservable().ToStrings().LastAsync());
        }

    }
}
