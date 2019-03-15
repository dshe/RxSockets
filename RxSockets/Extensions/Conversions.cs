using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;

#nullable enable

namespace RxSockets
{
    public static class ConversionsEx
    {
        public static byte[] ToByteArray(this string s) => Encoding.UTF8.GetBytes(s + "\0");

        public static IEnumerable<string> ToStrings(this IEnumerable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (var ms = new MemoryStream())
            {
                foreach (var b in source)
                {
                    if (b == 0)
                    {
                        yield return GetString(ms);
                        ms.SetLength(0);
                    }
                    else
                        ms.WriteByte(b);
                }
                if (ms.Position != 0)
                    throw new InvalidDataException("No termination.");
            }
        }

        public static IObservable<string> ToStrings(this IObservable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var ms = new MemoryStream();

            return Observable.Create<string>(observer =>
            {
                return source.Subscribe(
                    onNext: b =>
                    {
                        if (b == 0)
                        {
                            observer.OnNext(GetString(ms));
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
                            observer.OnError(new InvalidDataException("No termination."));
                    });
            });
        }

        private static string GetString(in MemoryStream ms) =>
            Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
    }
}
