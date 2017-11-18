using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace RxSocket.Tests.Utility
{
    public static class NetworkUtility
    {
        private static Random Random = new Random();

        public static Socket CreateSocket() => new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
        {
            DualMode = true, NoDelay = true
        };

        public static int GetRandomUnusedPort()
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
              .GetActiveTcpListeners().Any(x => x.Port == port);
    }
}
