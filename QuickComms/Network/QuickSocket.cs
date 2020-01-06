﻿using System;
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

        private const string SocketNullErrorMessage = "Can't complete request because the Socket is null.";

        public async Task ConnectToPrimaryAddressAsync(bool startListening)
        {
            if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

            await SockLock.WaitAsync().ConfigureAwait(false);
            if (!Connected)
            {
                Connected = true;

                await Socket
                    .ConnectAsync(DnsEntry.PrimaryAddress, DnsEntry.Port)
                    .ConfigureAwait(false);

                if (startListening)
                {
                    Socket.Bind(DnsEntry.Endpoint);
                    Socket.Listen(100);
                }
            }
            SockLock.Release();
        }

        public async Task ConnectToAddressesAsync(bool startListening)
        {
            if (Socket == null) throw new InvalidOperationException(SocketNullErrorMessage);

            await SockLock.WaitAsync().ConfigureAwait(false);
            if (!Connected)
            {
                Connected = true;

                await Socket
                    .ConnectAsync(DnsEntry.Addresses, DnsEntry.Port)
                    .ConfigureAwait(false);

                if (startListening)
                {
                    Socket.Bind(DnsEntry.Endpoint);
                    Socket.Listen(100);
                }
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
