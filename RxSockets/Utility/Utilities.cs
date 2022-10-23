using System.Net;
using System.Net.Sockets;
namespace RxSockets;

internal static class Utilities
{
    internal static IPEndPoint CreateIPEndPointOnPortZero() => new(IPAddress.IPv6Loopback, 0);

    internal static Socket CreateSocket() => new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
}
