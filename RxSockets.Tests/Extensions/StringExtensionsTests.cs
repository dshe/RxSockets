using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RxSockets.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData(new byte[] { 0 }, "")]
    [InlineData(new byte[] { 0, 0 }, "\0")]
    [InlineData(new byte[] { 65, 0 }, "A")]
    [InlineData(new byte[] { 65, 66, 0 }, "AB")]
    public void T01_ToByteArray(byte[] encoded, string str) =>
        Assert.Equal(encoded, str.ToByteArray());

    [Fact]
    public async Task T02_To_Strings()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            await ((byte[]?)null).ToObservable().ToStrings().ToList()); // should have warning?
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        // no termination
        Assert.Throws<InvalidDataException>(() => new byte[] { 65 }.ToStrings().ToList());
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await new byte[] { 65 }.ToObservable().ToStrings().ToList());

        var observable = Observable.Throw<byte>(new ArithmeticException()).ToStrings();
        await Assert.ThrowsAsync<ArithmeticException>(async () => await observable);
    }

    [Theory]
    [InlineData(new string[] { }, new byte[] { })]
    [InlineData(new[] { "" }, new byte[] { 0 })]
    [InlineData(new[] { "A" }, new byte[] { 65, 0 })]
    [InlineData(new[] { "AB" }, new byte[] { 65, 66, 0 })]
    [InlineData(new[] { "", "" }, new byte[] { 0, 0 })]
    [InlineData(new[] { "A", "B" }, new byte[] { 65, 0, 66, 0 })]
    public async Task T03_To_Strings(IEnumerable<string> strings, byte[] bytes)
    {
        Assert.Equal(strings, bytes.ToStrings().ToList());
        Assert.Equal(strings, await bytes.ToObservable().ToStrings().ToList());
    }
}
