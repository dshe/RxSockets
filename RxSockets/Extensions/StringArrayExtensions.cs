using System.IO;
using System.Reactive.Linq;
using System.Text;
namespace RxSockets;

public static partial class Extension
{
    /// <summary>
    /// Transform a sequence of byte arrays into a sequence of string arrays.
    /// </summary>
    public static IEnumerable<string[]> ToStringArrays(this IEnumerable<byte[]> source) =>
        source.Select(buffer => buffer.ToStringArray());

    /// <summary>
    /// Transform a sequence of byte arrays into a sequence of string arrays.
    /// </summary>
    public static IAsyncEnumerable<string[]> ToStringArrays(this IAsyncEnumerable<byte[]> source) =>
        source.Select(bytes => bytes.ToStringArray());

    /// <summary>
    /// Transform a sequence of byte arrays into a sequence of string arrays.
    /// </summary>
    public static IObservable<string[]> ToStringArrays(this IObservable<byte[]> source) =>
        source.Select(buffer => buffer.ToStringArray());

    /// <summary>
    /// Transform a byte array into an array of strings.
    /// </summary>
    private static string[] ToStringArray(this byte[] buffer)
    {
        int length = buffer.Length;
        if (length == 0 || buffer[length - 1] != 0)
            throw new InvalidDataException("ToStringArray: no termination.");
        return Encoding.UTF8.GetString(buffer, 0, length - 1).Split('\0');
    }
}

