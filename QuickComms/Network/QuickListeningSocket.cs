using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QuickComms.Network
{
    public class QuickListeningSocket
    {
        public DnsEntry DnsEntry { get; set; }
        public Socket Socket { get; set; }
        private SemaphoreSlim SockLock { get; } = new SemaphoreSlim(1, 1);

        public bool Listening { get; private set; }

        private const string SocketNullErrorMessage = "Can't complete request because the Socket is null.";

        public async Task BindSocketToAddressAsync(int pendingConnections)
        {
            if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

            await SockLock.WaitAsync().ConfigureAwait(false);

            Socket.Bind(DnsEntry.Endpoint);
            Socket.Listen(pendingConnections);

            Socket.NoDelay = true;

            Listening = true;

            SockLock.Release();
        }

        public async Task ShutdownAsync()
        {
            await SockLock.WaitAsync().ConfigureAwait(false);
            if (Listening)
            {
                Socket.Shutdown(SocketShutdown.Receive);
                Socket.Close();
                Listening = false;
            }
            SockLock.Release();
        }
    }
}
