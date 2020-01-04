using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets
{
    internal static class SocketConnector
    {
        internal static async Task<Socket> ConnectAsync(IPEndPoint endPoint, ILogger logger, int timeout = -1, CancellationToken ct = default)
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
                    if (!await semaphore.WaitAsync(timeout, ct).ConfigureAwait(false))
                        throw new SocketException((int)SocketError.TimedOut);

                if (args.SocketError != SocketError.Success)
                    throw new SocketException((int)args.SocketError);

                //logger.LogInformation($"Socket at {socket.LocalEndPoint} connected to {socket.RemoteEndPoint}.");
                return socket;
            }
            catch (SocketException e)
            {
                logger.LogInformation($"Socket at {socket.LocalEndPoint} could not connect to {endPoint}.");
                logger.LogInformation(e, Enum.GetName(typeof(SocketError), e.ErrorCode));
                throw;
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation($"Socket at {socket.LocalEndPoint} could not connect to {endPoint}.");
                logger.LogInformation(e, "Exception");
                throw;
            }
            catch (Exception e)
            {
                logger.LogInformation($"Socket at {socket.LocalEndPoint} could not connect to {endPoint}.");
                logger.LogInformation(e, "Exception");
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
