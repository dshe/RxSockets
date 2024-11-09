using System.IO;
using System.Reactive.Linq;
namespace RxSockets;

public static partial class Extension
{
    /// <summary>
    /// Prepend a 4 byte payload length to a byte array.
    /// </summary>
    public static byte[] ToByteArrayWithLengthPrefix(this byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        byte[] buffer = new byte[source.Length + 4];
        source.CopyTo(buffer, 4);
        EncodeMessageLength(buffer);
        return buffer;
    }

    /////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Transform a sequence of bytes with a length prefix into a sequence of byte arrays.
    /// </summary>
    public static IEnumerable<byte[]> ToArraysFromBytesWithLengthPrefix(this IEnumerable<byte> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int length = -1;
        using MemoryStream ms = new();
        foreach (byte b in source)
        {
            ms.WriteByte(b);
            if (length == -1 && ms.Position == 4)
            {
                length = DecodeMessageLength(ms);
                ms.SetLength(0);
            }
            else if (length == ms.Length)
            {
                yield return ms.ToArray(); // array copy
                length = -1;
                ms.SetLength(0);
            }
        }
        if (ms.Position != 0)
            throw new InvalidDataException($"Invalid length: {length}.");
    }

    /// <summary>
    /// Transform a sequence of bytes with a length prefix into a sequence of byte arrays.
    /// </summary>
    public static async IAsyncEnumerable<byte[]> ToArraysFromBytesWithLengthPrefix(this IAsyncEnumerable<byte> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        int length = -1;
        using MemoryStream ms = new();
        await foreach (byte b in source.ConfigureAwait(false))
        {
            ms.WriteByte(b);
            if (length == -1 && ms.Position == 4)
            {
                length = DecodeMessageLength(ms);
                ms.SetLength(0);
            }
            else if (length == ms.Length)
            {
                yield return ms.ToArray(); // array copy
                length = -1;
                ms.SetLength(0);
            }
        }
        if (ms.Position != 0)
            throw new InvalidDataException("ToArraysFromBytesWithLengthPrefix: invalid termination.");
    }

    /// <summary>
    /// Transform a sequence of bytes with a length prefix into a sequence of byte arrays.
    /// </summary>
    public static IObservable<byte[]> ToArraysFromBytesWithLengthPrefix(this IObservable<byte> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return Observable.Create<byte[]>(observer =>
        {
            int length = -1;
            MemoryStream ms = new();

            return source.Subscribe(
                onNext: b =>
                {
                    ms.WriteByte(b);
                    if (length == -1 && ms.Position == 4)
                    {
                        length = DecodeMessageLength(ms);
                        ms.SetLength(0);
                    }
                    else if (length == ms.Length)
                    {
                        observer.OnNext(ms.ToArray()); // array copy
                        length = -1;
                        ms.SetLength(0);
                    }
                },
                onError: (e) =>
                {
                    observer.OnError(e);
                    ms.Dispose();
                },

                onCompleted: () =>
                {
                    if (ms.Position == 0)
                        observer.OnCompleted();
                    else
                        observer.OnError(new InvalidDataException("ToArraysFromBytesWithLengthPrefix: incomplete."));
                    ms.Dispose();
                });
        });
    }

    // Encode 4 byte BigEndian integer length prefix. 
    private static void EncodeMessageLength(byte[] buffer)
    {
        int length = buffer.Length - 4;
        int i = IPAddress.HostToNetworkOrder(length);
        if (!BitConverter.TryWriteBytes(buffer, i))
            throw new InvalidDataException($"TryWriteBytes.");
    }

    private static int DecodeMessageLength(MemoryStream ms)
    {
        byte[] buffer = ms.GetBuffer();
        int i = BitConverter.ToInt32(buffer, 0);
        int length = IPAddress.NetworkToHostOrder(i);
        if (length <= 0)
            throw new InvalidDataException($"Invalid length: {length}.");
        return length;
    }
}
