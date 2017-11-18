using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace RxSocket
{
    public static class StringsEx
    {
        internal static string ToHexString(this IEnumerable<byte> buffer, int offset, int length) =>
            BitConverter.ToString(buffer.Skip(offset).Take(length).ToArray());

        public static byte[] ToBytes(this string str)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            return Encoding.UTF8.GetBytes(str + "\0");
        }

        public static IObservable<string> ToStrings(this IObservable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var ms = new MemoryStream();

            return Observable.Create<string>(observer =>
            {
                return source.Subscribe(onNext: b =>
                {
                    if (b == 0)
                    {
                        observer.OnNext(Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position));
                        ms.Position = 0;
                    }
                    else
                        ms.WriteByte(b);

                }, onError: observer.OnError, onCompleted: observer.OnCompleted);
            });
        }
    }
}
