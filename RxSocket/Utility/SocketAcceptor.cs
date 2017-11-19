using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

namespace RxSocket
{
    internal class SocketAcceptor
    {
        // Backlog specifies the number of pending connections allowed before a busy error is returned to the client.
        private readonly int Backlog;
        private readonly Socket Socket;
        private int Listening;

        internal SocketAcceptor(Socket socket, int backlog = 10)
        {
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            Backlog = backlog;
        }

        internal (SocketError error, Socket xsocket) Accept()
        {
            try
            {
                if (Interlocked.CompareExchange(ref Listening, 1, 0) == 0)
                    Socket.Listen(Backlog);

                var semaphore = new SemaphoreSlim(0, 1);
                var args = new SocketAsyncEventArgs();
                args.Completed += (sender, a) => semaphore.Release();

                if (Socket.AcceptAsync(args))
                    semaphore.Wait();

                return (args.SocketError, args.SocketError == SocketError.Success ? args.AcceptSocket : null);
            }
            catch (SocketException se)
            {
                return (se.SocketErrorCode, null);
            }
            catch (ObjectDisposedException)
            {
                return (SocketError.Shutdown, null);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Accept() unhandled exception: " + e.Message);
                throw;
            }
        }
    }
}
