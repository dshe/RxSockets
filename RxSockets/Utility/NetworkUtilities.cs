using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

#nullable enable

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
            while (true)
            {
                var port = RandomInt(1024, 65535);
                if (!IsPortUsed(port))
                    return port;
            }
        }

        private static bool IsPortUsed(int port) =>
            IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(ep => ep.Port == port);

        private static int RandomInt(int min = int.MinValue, int max = int.MaxValue)
        {
            var buffer = GetRandomBytes(4);
            int result = BitConverter.ToInt32(buffer, 0);
            return new Random(result).Next(min, max);
        }

        private static byte[] GetRandomBytes(int bytes)
        {
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                var buffer = new byte[bytes];
                rng.GetBytes(buffer);
                return buffer;
            }
        }

    }
}
