using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace RxSockets.Tests
{
    public static class TestUtilities
    {
        private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();
        private static readonly object Locker = new();

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
            RandomNumberGenerator.GetBytes(buffer, 0, bytes);
            return buffer;
        }
    }
}
