using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert a string to a byte array.
        /// </summary>
        public static byte[] ToByteArray(this string source) =>
            Encoding.UTF8.GetBytes(source + "\0");

        /// <summary>
        /// Convert a sequence of strings to a byte array.
        /// </summary>
        public static byte[] ToByteArray(this IEnumerable<string> source) =>
            source.Select(s => s.ToByteArray()).SelectMany(x => x).ToArray();

        ///////////////////////////////////////////////////////////////

        /// <summary>
        /// Convert a sequence of bytes into a sequence of strings.
        /// </summary>
        public static IEnumerable<string> ToStrings(this IEnumerable<byte> source)
        {
            using var ms = new MemoryStream();
            foreach (byte b in source)
            {
                if (b == 0)
                {
                    yield return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
                    ms.SetLength(0);
                }
                else
                    ms.WriteByte(b);
            }
            if (ms.Position != 0)
                throw new InvalidDataException("ToStrings: no termination(1).");
        }

        /// <summary>
        /// Transform a sequence of bytes into a sequence of strings.
        /// </summary>
        public static async IAsyncEnumerable<string> ToStrings(this IAsyncEnumerable<byte> source, [EnumeratorCancellation] CancellationToken ct = default)
        {
            using MemoryStream ms = new();
            await foreach (byte b in source.WithCancellation(ct).ConfigureAwait(false))
            {
                if (b != 0)
                {
                    ms.WriteByte(b);
                    continue;
                }
                yield return Encoding.UTF8.GetString(ms.ToArray());
                ms.SetLength(0);
            }
            if (ms.Position != 0)
                throw new InvalidDataException("ToStrings: invalid termination.");
        }

        /// <summary>
        /// Convert a sequence of bytes into a sequence of strings.
        /// </summary>
        public static IObservable<string> ToStrings(this IObservable<byte> source)
        {
            var ms = new MemoryStream();
            return Observable.Create<string>(observer =>
            {
                return source.Subscribe(
                    onNext: b =>
                    {
                        if (b == 0)
                        {
                            string s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
                            observer.OnNext(s);
                            ms.SetLength(0);
                        }
                        else
                            ms.WriteByte(b);
                    },
                    onError: observer.OnError,
                    onCompleted: () =>
                    {
                        if (ms.Position == 0)
                            observer.OnCompleted();
                        else
                            observer.OnError(new InvalidDataException("ToStrings: invalid termination."));
                    });
            });
        }
    }
}
