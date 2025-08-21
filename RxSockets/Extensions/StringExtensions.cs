using System.IO;
using System.Reactive.Linq;
using System.Text;
namespace RxSockets;

public static partial class Extension
{
    /// <summary>
    /// Convert a string to a byte array.
    /// </summary>
    public static byte[] ToByteArray(this string source) =>
        Encoding.UTF8.GetBytes(source + '\0');

    /// <summary>
    /// Convert a sequence of strings to a byte array.
    /// </summary>
    public static byte[] ToByteArray(this IEnumerable<string> source) =>
        [.. source.SelectMany(s => s.ToByteArray())];

    ///////////////////////////////////////////////////////////////

    /// <summary>
    /// Convert a sequence of bytes into a sequence of strings.
    /// </summary>
    public static IEnumerable<string> ToStrings(this IEnumerable<byte> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using MemoryStream ms = new();
        foreach (byte b in source)
        {
            if (b != 0)
            {
                ms.WriteByte(b);
                continue;
            }
            string s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
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
        using MemoryStream ms = new();
        await foreach (byte b in source.ConfigureAwait(false))
        {
            if (b != 0)
            {
                ms.WriteByte(b);
                continue;
            }
            string s = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Position);
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
                    ms.SetLength(0);
                    observer.OnNext(s);
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
                        observer.OnError(new InvalidDataException("ToStrings: invalid termination."));
                    ms.Dispose();
                });
        });
    }
}
