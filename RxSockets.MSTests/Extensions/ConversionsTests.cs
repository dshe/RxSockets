using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    [TestClass]
    public class ConversionsTest
    {
        [DataRow(new byte[] { 0 }, "" )]
        [DataRow(new byte[] { 0, 0 }, "\0" )]
        [DataRow(new byte[] { 65, 0 }, "A" )]
        [DataRow(new byte[] { 65, 66, 0 }, "AB" )]
        [DataTestMethod]
        public void T01_ToByteArray(byte[] encoded, string str) =>
            CollectionAssert.AreEqual(encoded, ConversionsEx.ToByteArray(str));

        /////////////////////////////////////////////////////////////////////

        [TestMethod]
        public async Task T02_ToStrings()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
                await ((byte[]?)null).ToObservable().ToStrings().ToList()); // should have warning?

            // no termination
            Assert.ThrowsException<InvalidDataException>(() => new byte[] { 65 }.ToStrings().ToList());
            await Assert.ThrowsExceptionAsync<InvalidDataException>(async () => 
                await new byte[] { 65 }.ToObservable().ToStrings().ToList());

            var observable = Observable.Throw<byte>(new ArithmeticException()).ToStrings();
            await Assert.ThrowsExceptionAsync<ArithmeticException>(async () => await observable);
        }

        [DataTestMethod]
        [DataRow(new string[] { },   new byte[] { })]
        [DataRow(new[] { "" },       new byte[] { 0 })]
        [DataRow(new[] { "A" },      new byte[] { 65, 0 })]
        [DataRow(new[] { "AB" },     new byte[] { 65, 66, 0 })]
        [DataRow(new[] { "", "" },   new byte[] { 0, 0 })]
        [DataRow(new[] { "A", "B" }, new byte[] { 65, 0, 66, 0 })]
        public async Task T03_ToStrings(string[] strings, byte[] bytes)
        {
            CollectionAssert.AreEqual(strings, bytes.ToStrings().ToList());
            CollectionAssert.AreEqual(strings, await bytes.ToObservable().ToStrings().ToArray());
        }
    }
}
