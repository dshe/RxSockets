namespace RxSockets;

internal static class Utilities
{
    internal static IPEndPoint CreateIPEndPointOnPort(int port) => new(IPAddress.Loopback, port);

    internal static Socket CreateSocket() => new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
}
