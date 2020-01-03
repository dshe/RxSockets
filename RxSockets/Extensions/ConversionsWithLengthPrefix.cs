using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reactive.Linq;

namespace RxSockets
{
    // Encode strings with a 4 byte BigEndian integer length prefix.
    public static class ConversionsWithLengthPrefixEx
    {
        public static byte[] ToByteArrayWithLengthPrefix(this IEnumerable<string> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            using var ms = new MemoryStream() { Position = 4 };
            foreach (var s in source)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    var buffer = Encoding.UTF8.GetBytes(s);
                    ms.Write(buffer, 0, buffer.Length);
                }
                ms.WriteByte(0); // null or empty
            }
            return GetBytes(ms);
        }

        private static byte[] GetBytes(in MemoryStream ms)
        {
            if (ms == null)
                throw new ArgumentNullException(nameof(ms));
            var length = Convert.ToInt32(ms.Position) - 4;
            var prefix = IPAddress.HostToNetworkOrder(length);
            ms.Position = 0;
            ms.Write(BitConverter.GetBytes(prefix), 0, 4);
            return ms.ToArray(); // array copy
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static IEnumerable<byte[]> ToByteArrayOfLengthPrefix(this IEnumerable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var length = -1;

            using var ms = new MemoryStream();
            foreach (var b in source)
            {
                ms.WriteByte(b);
                if (length == -1 && ms.Position == 4)
                {
                    length = GetMessageLength(ms);
                    ms.SetLength(0);
                }
                else if (ms.Length == length)
                {
                    yield return ms.ToArray(); // array copy
                    length = -1;
                    ms.SetLength(0);
                }
            }
            if (ms.Position != 0)
                throw new InvalidDataException("Incomplete.");
        }

        public static IObservable<byte[]> ToByteArrayOfLengthPrefix(this IObservable<byte> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var length = -1;
            var ms = new MemoryStream();

            return Observable.Create<byte[]>(observer =>
            {
                return source.Subscribe(
                    onNext: b =>
                    {
                        ms.WriteByte(b);
                        if (length == -1 && ms.Position == 4)
                        {
                            length = GetMessageLength(ms);
                            ms.SetLength(0);
                        }
                        else if (length == ms.Length)
                        {
                            observer.OnNext(ms.ToArray()); // array copy
                            length = -1;
                            ms.SetLength(0);
                        }
                    }, 
                    onError: observer.OnError, 
                    onCompleted: () => 
                    {
                        if (ms.Position == 0)
                            observer.OnCompleted();
                        else
                            observer.OnError(new InvalidDataException("ToByteArrayOfLengthPrefix: incomplete."));
                    });
            });
        }

        private static int GetMessageLength(in MemoryStream ms)
        {
            var length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ms.GetBuffer(), 0));
            if (length <= 0)
                throw new InvalidOperationException($"Invalid length: {length}.");
            return length;
        }

        public static IEnumerable<string[]> ToStringArray(this IEnumerable<byte[]> source) =>
            source.Select(buffer => GetStringArray(buffer));

        public static IObservable<string[]> ToStringArray(this IObservable<byte[]> source) =>
            source.Select(buffer => GetStringArray(buffer));

        internal static string[] GetStringArray(in byte[] buffer) // keep internal for testing
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            var length = buffer.Length;
            if (length == 0 || buffer[length - 1] != 0)
                throw new InvalidDataException("GetStringArray: no termination.");
            return Encoding.UTF8.GetString(buffer, 0, length - 1).Split('\0');
        }
    }
}
