using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class StringExtensions
    {
        public static byte[] ToBuffer(this string source) =>
            Encoding.UTF8.GetBytes(source + "\0");

        public static async Task<string> ToStringAsync(this IAsyncEnumerable<byte> source)
        {
            byte[] array = await source
                .TakeWhile(b => b != 0)
                .ToArrayAsync()
                .ConfigureAwait(false);

            return Encoding.UTF8.GetString(array);
        }

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
                            observer.OnError(new InvalidDataException("ToStrings: no termination(2)."));
                    });
            });
        }
    }
}
