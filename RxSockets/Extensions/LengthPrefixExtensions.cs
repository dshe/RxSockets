using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace RxSockets
{
    public static class LengthPrefixExtensions
    {
        public static byte[] InsertLengthPrefix(this byte[] sourceArray)
        {
            int length = sourceArray.Length;
            byte[] prefixArray = SetMessageLength(length);
            byte[] newArray = new byte[length + 4];
            Array.Copy(prefixArray, 0, newArray, 0, 4);
            Array.Copy(sourceArray, 0, newArray, 4, length);
            return newArray;
        }

        public static async Task<byte[]> RemoveLengthPrefix(this IAsyncEnumerable<byte> bytes)
        {
            byte[] prefix = await bytes.Take(4).ToArrayAsync().ConfigureAwait(false);
            int messageLength = GetMessageLength(prefix);
            byte[] message = await bytes.Take(messageLength).ToArrayAsync().ConfigureAwait(false);
            return message;
        }

        public static IEnumerable<byte[]> RemoveLengthPrefix(this IEnumerable<byte> source)
        {
            int length = -1;
            using var ms = new MemoryStream();

            foreach (var b in source)
            {
                ms.WriteByte(b);
                if (length == -1 && ms.Position == 4)
                {
                    length = GetMessageLength(ms.GetBuffer());
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

        public static IObservable<byte[]> RemoveLengthPrefix(this IObservable<byte> source)
        {
            int length = -1;
            var ms = new MemoryStream();

            return Observable.Create<byte[]>(observer =>
            {
                return source.Subscribe(
                    onNext: b =>
                    {
                        ms.WriteByte(b);
                        if (length == -1 && ms.Position == 4)
                        {
                            length = GetMessageLength(ms.GetBuffer());
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
                            observer.OnError(new InvalidDataException("RemoveLengthPrefix: incomplete."));
                    });
            });
        }

        // Encode using a 4 byte BigEndian integer length prefix. 
        internal static byte[] SetMessageLength(int length)
        {
            int i = IPAddress.HostToNetworkOrder(length);
            return BitConverter.GetBytes(i);
        }

        internal static int GetMessageLength(in byte[] bytes)
        {
            int i = BitConverter.ToInt32(bytes, 0);
            int length = IPAddress.NetworkToHostOrder(i);
            if (length <= 0)
                throw new InvalidOperationException($"Invalid length: {length}.");
            return length;
        }
    }
}
