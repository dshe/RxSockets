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
        public static Socket CreateSocket() =>
            new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        public static IPEndPoint GetEndPointOnLoopbackRandomPort() =>
            new IPEndPoint(IPAddress.IPv6Loopback, GetRandomAvailablePort());

        public static int GetRandomAvailablePort()
        {
            int port;
            do
            {
                port = RandomInt(1024, 65535);
            } while (IsPortUsed(port));
            return port;
        }

        private static bool IsPortUsed(int port) =>
            IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(ep => ep.Port == port);

        private static int RandomInt(int min, int max)
        {
            var buffer = GetRandomBytes(4);
            int result = BitConverter.ToInt32(buffer, 0);
            return new Random(result).Next(min, max);
        }

        private static byte[] GetRandomBytes(int bytes)
        {
            var buffer = new byte[bytes];
            using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(buffer);
            return buffer;
        }
    }
}
