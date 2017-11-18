using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;

namespace RxSocket.Tests
{
    public class StringsTest
    {
        [Fact]
        public async Task TestError()
        {
            var observable = Observable.Throw<byte>(new ArithmeticException()).ToStrings();
            await Assert.ThrowsAsync<ArithmeticException>(async () => await observable);
        }

        [Fact]
        public async Task TestTimeout()
        {
            var observable = Observable.Never<byte>().ToStrings();
            await Assert.ThrowsAsync<TimeoutException>(async () => await observable.Timeout(TimeSpan.FromMilliseconds(1)));
        }

        [Fact]
        public async Task TestEmpty()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Observable.Empty<byte>().ToStrings());
        }

        [Fact]
        public async Task TestObservableToStrings()
        {
            Assert.Equal("", await new byte[] { 0 }.ToObservable().ToStrings().SingleAsync());
            Assert.Equal("A", await new byte[] { 65, 0 }.ToObservable().ToStrings().SingleAsync());
            Assert.Equal("AB", await new byte[] { 65, 66, 0 }.ToObservable().ToStrings().SingleAsync());
            Assert.Equal(new List<string>() { "A", "B" }, await new byte[] { 65, 0, 66, 0 }.ToObservable().ToStrings().ToList());
            Assert.Equal(new List<string>() { "A", "" }, await new byte[] { 65, 0, 0 }.ToObservable().ToStrings().ToList());
        }

    }
}

