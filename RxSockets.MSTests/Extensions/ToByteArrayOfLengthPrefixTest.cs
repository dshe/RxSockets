using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace RxSockets.MSTests
{
    [TestClass]
    public class ConversionsFromByteArrayOfLengthPrefixTest
    {
        [TestMethod]
        public async Task T01()
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await (new byte[] { 0, 0, 0, 0, 0 }).ToObservable().RemoveLengthPrefix().FirstOrDefaultAsync());
        }

        [DataRow(new byte[] { 0 }, new byte[] { 0, 0, 0, 1, 0 })]
        [DataRow(new byte[] { 65, 0 }, new byte[] { 0, 0, 0, 2, 65, 0 })]
        [DataRow(new byte[] { 65, 0, 66, 0 }, new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 })]
        [DataTestMethod]
        public async Task T02(byte[] result, byte[] bytes)
        {
            var removed = await bytes.ToObservable().RemoveLengthPrefix();
            CollectionAssert.AreEqual(result, removed);
        }


        [TestMethod]
        public void T03()
        {
            Assert.ThrowsException<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { }));
            Assert.ThrowsException<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { 65 }));
            Assert.ThrowsException<InvalidDataException>(() => ConversionsWithLengthPrefixEx.GetStringArray(new byte[] { 65, 0, 65 }));
        }

        [DataRow(new byte[] { 0 }, "" )]
        [DataRow(new byte[] { 0, 0 }, "", "" )]
        [DataRow(new byte[] { 65, 0 }, "A" )]
        [DataRow(new byte[] { 65, 0, 65, 0, 0 }, "A", "A", "")]
        [TestMethod]
        public void T04(byte[] bytes, params string[] strings)
        {
            CollectionAssert.AreEqual(strings, ConversionsWithLengthPrefixEx.GetStringArray(bytes));
        }
    }
}
