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
            MemoryStream ms = new();
            foreach (byte b in source)
            {
                if (b != 0)
                {
                    ms.WriteByte(b);
                    continue;
                }
                var s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
                ms.SetLength(0);
                yield return s;
            }
            if (ms.Position != 0)
                throw new InvalidDataException("ToStrings: no termination(1).");
        }

        /// <summary>
        /// Transform a sequence of bytes into a sequence of strings.
        /// </summary>
        public static async IAsyncEnumerable<string> ToStrings(this IAsyncEnumerable<byte> source)
        {
            MemoryStream ms = new();
            await foreach (byte b in source.ConfigureAwait(false))
            {
                if (b != 0)
                {
                    ms.WriteByte(b);
                    continue;
                }
                var s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
                ms.SetLength(0);
                yield return s;
            }
            if (ms.Position != 0)
                throw new InvalidDataException("ToStrings: invalid termination.");
        }

        /// <summary>
        /// Convert a sequence of bytes into a sequence of strings.
        /// </summary>
        public static IObservable<string> ToStrings(this IObservable<byte> source)
        {
            MemoryStream ms = new();
            return Observable.Create<string>(observer =>
            {
                return source.Subscribe(
                    onNext: b =>
                    {
                        if (b != 0)
                        {
                            ms.WriteByte(b);
                            return;
                        }
                        string s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
                        observer.OnNext(s);
                        ms.SetLength(0);
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
