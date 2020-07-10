using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RxSockets.MSTests
{
    /*
    public class ConversionsToByteArrayWithLengthPrefixTest
    {
        [DataRow(new byte[] { 0, 0, 0, 0 }, new string[] { })]
        [DataRow(new byte[] { 0, 0, 0, 1, 0 }, new[] { "" })]
        [DataRow(new byte[] { 0, 0, 0, 2, 0, 0 }, new[] { "\0" })]
        [DataRow(new byte[] { 0, 0, 0, 2, 65, 0 }, new[] { "A" })]
        [DataRow(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, new[] { "A", "B" })]
        [DataTestMethod]
        public void T02(byte[] encoded, IEnumerable<string> str)
        {
            Assert.AreEqual(encoded, ConversionsWithLengthPrefixEx.ToByteArrayWithLengthPrefix(str));
        }
    }
    */

    [TestClass]
    public class ConversionsToByteArrayWithLengthPrefixTest2
    {
        [DataRow(new byte[] { 0, 0, 0, 0 })]
        [DataRow(new byte[] { 0, 0, 0, 1, 0 }, "" )]
        [DataRow(new byte[] { 0, 0, 0, 2, 0, 0 }, "\0" )]
        [DataRow(new byte[] { 0, 0, 0, 2, 65, 0 }, "A" )]
        [DataRow(new byte[] { 0, 0, 0, 4, 65, 0, 66, 0 }, "A", "B" )]
        [DataTestMethod]
        public void T02(byte[] encoded, params string[] str)
        {
            CollectionAssert.AreEqual(encoded, ConversionsWithLengthPrefixEx.ToByteArrayWithLengthPrefix(str));
        }
    }

}
