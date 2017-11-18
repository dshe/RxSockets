using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace RxSocket
{
    /*
     * Encode/decode strings with a 4 byte integer length prefix (BigEndian).
     */
    public static class StringMessageEx
    {
        public static byte[] EncodeToBytes(this IEnumerable<string> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using (var ms = new MemoryStream() { Position = 4 })
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var buffer = Encoding.UTF8.GetBytes(enumerator.Current);
                    ms.Write(buffer, 0, buffer.Length);
                    ms.WriteByte(0);
                }
                return CreateMessage(ms);
            }
        }

        private static byte[] CreateMessage(MemoryStream ms)
        {
            var payloadLength = Convert.ToInt32(ms.Position) - 4;
            var prefix = IPAddress.HostToNetworkOrder(payloadLength);
            ms.Position = 0;
            ms.Write(BitConverter.GetBytes(prefix), 0, 4);
            return ms.ToArray(); // array copy
        }

        public static IObservable<string[]> DecodeToStrings(this IObservable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var payloadLength = -1;
            var ms = new MemoryStream();
            return Observable.Create<string[]>(observer =>
            {
                return source.Subscribe(onNext: b =>
                {
                    ms.WriteByte(b);
                    if (payloadLength == -1 && ms.Position == 4)
                    {
                        payloadLength = GetMessageLength(ms);
                        ms.SetLength(0);
                    }
                    else if (payloadLength == ms.Length)
                    {
                        observer.OnNext(GetStrings(ms));
                        payloadLength = -1;
                    }
                }, onError: observer.OnError, onCompleted: observer.OnCompleted);
            });
        }

        private static string[] GetStrings(MemoryStream ms)
        {
            var position = (int)ms.Position;
            if (position == 0)
                throw new InvalidDataException("No data.");
            var buffer = ms.GetBuffer();
            if (buffer[position - 1] != 0)
                throw new InvalidDataException("String is not terminated.");
            return GetStrings(buffer, 0, position - 1);
        }

        private static string[] GetStrings(byte[] buffer, int index, int count)
            => Encoding.UTF8.GetString(buffer, index, count).Split('\0');

        private static int GetMessageLength(MemoryStream ms)
            => IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ms.GetBuffer(), 0));
    }
}
