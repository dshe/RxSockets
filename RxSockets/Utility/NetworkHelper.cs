using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

#nullable enable

namespace RxSockets
{
    public static class NetworkHelper
    {
        private static Random Random = new Random();

        public static Socket CreateSocket() =>
            new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        public static IPEndPoint GetEndPointOnLoopbackRandomPort() =>
            new IPEndPoint(IPAddress.IPv6Loopback, GetRandomAvailablePort());

        public static int GetRandomAvailablePort()
        {
            while (true)
            {
                var port = Random.Next(1024, 65535);
                if (!IsPortUsed(port))
                    return port;
            }
        }

        private static bool IsPortUsed(int port) =>
            IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpListeners().Any(ep => ep.Port == port);
    }
}
