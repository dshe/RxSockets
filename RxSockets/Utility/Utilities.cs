using System.Net.Sockets;

namespace RxSockets
{
    public static class Utilities
    {
        internal static Socket CreateSocket() => new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
    }
}
