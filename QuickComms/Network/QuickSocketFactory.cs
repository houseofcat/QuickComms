using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickComms.Network
{
    public class QuickSocketFactory
    {
        private NetCaching DnsCaching { get; }
        private ConcurrentDictionary<string, QuickSocket> Sockets { get; }
        private const string SocketKeyFormat = "{0}{1}:{2}:{3}";
        private const int CachingExpiryInHours = 1;

        public QuickSocketFactory()
        {
            DnsCaching = new NetCaching(TimeSpan.FromHours(CachingExpiryInHours));
            Sockets = new ConcurrentDictionary<string, QuickSocket>();
        }

        private string GetSocketKey(ProtocolType protocolType, SocketType socketType, string hostNameOrAddresss, int bindingPort)
        {
            return string.Format(SocketKeyFormat, protocolType, socketType, hostNameOrAddresss, bindingPort);
        }

        public async ValueTask<QuickSocket> GetTcpStreamSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
        {
            var key = GetSocketKey(ProtocolType.Tcp, SocketType.Stream, hostNameOrAddresss, bindingPort);

            if (Sockets.ContainsKey(key))
            {
                return Sockets[key];
            }
            else
            {
                var dnsEntry = await DnsCaching.GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress).ConfigureAwait(false);
                var quickSocket = new QuickSocket
                {
                    Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp),
                    DnsEntry = dnsEntry,
                };

                Sockets[key] = quickSocket;

                return quickSocket;
            }
        }

        public async ValueTask<QuickSocket> GetUdpStreamSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false)
        {
            var key = GetSocketKey(ProtocolType.Udp, SocketType.Stream, hostNameOrAddresss, bindingPort);

            if (Sockets.ContainsKey(key))
            {
                return Sockets[key];
            }
            else
            {
                var dnsEntry = await DnsCaching.GetDnsEntryAsync(hostNameOrAddresss, bindingPort, overideAsLocal, verbatimAddress).ConfigureAwait(false);
                var quickSocket = new QuickSocket
                {
                    Socket = new Socket(dnsEntry.PrimaryAddress.AddressFamily, SocketType.Stream, ProtocolType.Udp),
                    DnsEntry = dnsEntry,
                };

                Sockets[key] = quickSocket;

                return quickSocket;
            }
        }
    }
}
