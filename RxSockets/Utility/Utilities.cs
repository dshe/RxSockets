using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace RxSockets
{
    public static class Utilities
    {
        private static readonly object Locker = new();

        public static Socket CreateSocket() =>
            new(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        public static IPEndPoint GetEndPointOnRandomLoopbackPort() =>
            new(IPAddress.IPv6Loopback, GetRandomAvailablePort());

        private static int GetRandomAvailablePort()
        {
            lock (Locker)
            {
                while (true)
                {
                    // IANA officially recommends 49152 - 65535 for the Ephemeral Ports.
                    int port = RandomInt(49152, 65535);
                    if (!IsPortUsed(port))
                        return port;
                }
            }
        }

        private static bool IsPortUsed(int port) =>
            IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(ep => ep.Port == port);

        private static int RandomInt(int min, int max)
        {
            byte[] buffer = GetRandomBytes(4);
            int result = BitConverter.ToInt32(buffer, 0);
            return new Random(result).Next(min, max);
        }

        private static byte[] GetRandomBytes(int bytes)
        {
            byte[] buffer = new byte[bytes];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }
    }
}
