using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class StringsWithLengthPrefixExtensions
    {
        public static byte[] ToBufferWithLengthPrefix(this IEnumerable<string> source)
        {
            using var ms = new MemoryStream() { Position = 4 };

            foreach (var s in source)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(s);
                    ms.Write(buffer, 0, buffer.Length);
                }
                ms.WriteByte(0); // null or empty
            }

            byte[] b = LengthPrefixExtensions.SetMessageLength((int)ms.Position - 4);
            ms.Position = 0;
            ms.Write(b, 0, 4);

            return ms.ToArray(); // array copy
        }

        public static async Task<string[]> ToStringsFromBufferWithLengthPrefixAsync(this IAsyncEnumerable<byte> bytes)
        {
            byte[] prefix = await bytes.Take(4).ToArrayAsync().ConfigureAwait(false);
            int length = LengthPrefixExtensions.GetMessageLength(prefix);
            byte[] message = await bytes.Take(length).ToArrayAsync().ConfigureAwait(false);
            return GetStrings(message);
        }

        public static IEnumerable<string[]> ToStrings(this IEnumerable<byte[]> source) =>
            source.Select(buffer => GetStrings(buffer));

        public static IObservable<string[]> ToStrings(this IObservable<byte[]> source) =>
            source.Select(buffer => GetStrings(buffer));

        internal static string[] GetStrings(byte[] buffer) // internal to allow testing
        {
            int length = buffer.Length;
            if (length == 0 || buffer[length - 1] != 0)
                throw new InvalidDataException("GetStringArray: no termination.");
            return Encoding.UTF8.GetString(buffer, 0, length - 1).Split('\0');
        }
    }
}
