using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RxSockets
{
    internal static class SocketConnector
    {
        internal static async Task<Socket> ConnectAsync(IPEndPoint endPoint, ILogger logger, CancellationToken ct = default)
        {
            var socket = Utilities.CreateSocket();
            var semaphore = new SemaphoreSlim(0, 1);
            void handler(object sender, SocketAsyncEventArgs a) => semaphore.Release();
            var args = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint
            };
            args.Completed += handler;

            try
            {
                ct.ThrowIfCancellationRequested();

                if (socket.ConnectAsync(args))
                    await semaphore.WaitAsync(ct).ConfigureAwait(false);

                if (args.SocketError != SocketError.Success)
                    throw new SocketException((int)args.SocketError);

                return socket;
            }
            catch (Exception e)
            {
                var msg = $"Socket at {socket.LocalEndPoint} could not connect to {endPoint}. {e.Message}";
                if (e is SocketException se)
                {
                    var errorName = "SocketException: " + Enum.GetName(typeof(SocketError), se.ErrorCode);
                    logger.LogDebug(e, $"{msg}. {errorName}.");
                }
                else if (e is OperationCanceledException)
                    logger.LogDebug(e, msg);
                else
                    logger.LogWarning(e, msg);
                throw;
            }
            finally
            {
                args.Completed -= handler;
                args.Dispose();
                semaphore.Dispose();
                if (args.SocketError != SocketError.Success)
                {
                    Socket.CancelConnectAsync(args);
                    socket.Dispose();
                }
            }
        }
    }
}
