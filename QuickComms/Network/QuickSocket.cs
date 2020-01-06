using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace QuickComms.Network
{
    public class QuickSocket
    {
        public DnsEntry DnsEntry { get; set; }
        public Socket Socket { get; set; }
        private SemaphoreSlim SockLock { get; } = new SemaphoreSlim(1, 1);
        public bool Connected { get; private set; }
        public bool Listening { get; private set; }

        private const string SocketNullErrorMessage = "Can't complete request because the Socket is null.";

        public async Task StartListeningToPrimaryAddressAsync(int pendingConnections)
        {
            await SockLock.WaitAsync().ConfigureAwait(false);

            Socket.Bind(DnsEntry.Endpoint);
            Socket.Listen(pendingConnections);
            Listening = true;

            SockLock.Release();
        }

        public async Task StopListeningAsync()
        {
            await SockLock.WaitAsync().ConfigureAwait(false);

            Socket.Close();
            Listening = false;

            SockLock.Release();
        }

        public async Task ConnectToPrimaryAddressAsync()
        {
            if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

            if (!Connected)
            {
                await Socket
                    .ConnectAsync(DnsEntry.PrimaryAddress, DnsEntry.Port)
                    .ConfigureAwait(false);

                Connected = true;
            }
            SockLock.Release();
        }

        public async Task ConnectToAddressesAsync()
        {
            if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

            await SockLock.WaitAsync().ConfigureAwait(false);
            if (!Connected)
            {
                await Socket
                    .ConnectAsync(DnsEntry.Addresses, DnsEntry.Port)
                    .ConfigureAwait(false);

                Connected = true;
            }
            SockLock.Release();
        }

        public async Task ShutdownAsync()
        {
            await SockLock.WaitAsync().ConfigureAwait(false);
            if (Connected)
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Connected = false;
            }
            SockLock.Release();
        }
    }
}
