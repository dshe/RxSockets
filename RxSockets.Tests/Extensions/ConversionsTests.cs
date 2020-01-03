using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RxSockets.Tests
{
    public class ConversionsTest
    {
        [Theory]
        [InlineData(new byte[] { 0 }, "" )]
        [InlineData(new byte[] { 0, 0 }, "\0" )]
        [InlineData(new byte[] { 65, 0 }, "A" )]
        [InlineData(new byte[] { 65, 66, 0 }, "AB" )]
        public void T01_ToByteArray(byte[] encoded, string str)
        {
            Assert.Equal(encoded, ConversionsEx.ToByteArray(str));
        }

        /////////////////////////////////////////////////////////////////////

        [Fact]
        public async Task T02_ToStrings()
        {
            byte[]? xxx = null;
            Assert.Throws<ArgumentNullException>(() => ConversionsEx.ToStrings(xxx).ToList()); // should have warning?

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await ((byte[]?)null).ToObservable().ToStrings().ToList()); // should have warning?

            // no termination
            Assert.Throws<InvalidDataException>(() => new byte[] { 65 }.ToStrings().ToList());
            await Assert.ThrowsAsync<InvalidDataException>(async () => 
                await new byte[] { 65 }.ToObservable().ToStrings().ToList());

            var observable = Observable.Throw<byte>(new ArithmeticException()).ToStrings();
            await Assert.ThrowsAsync<ArithmeticException>(async () => await observable);
        }

        [Theory]
        [InlineData(new string[] { },   new byte[] { })]
        [InlineData(new[] { "" },       new byte[] { 0 })]
        [InlineData(new[] { "A" },      new byte[] { 65, 0 })]
        [InlineData(new[] { "AB" },     new byte[] { 65, 66, 0 })]
        [InlineData(new[] { "", "" },   new byte[] { 0, 0 })]
        [InlineData(new[] { "A", "B" }, new byte[] { 65, 0, 66, 0 })]
        public async Task T03_ToStrings(IEnumerable<string> strings, byte[] bytes)
        {
            Assert.Equal(strings, bytes.ToStrings().ToList());
            Assert.Equal(strings, await bytes.ToObservable().ToStrings().ToList());
        }

    }
}
