using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickComms.Network
{
    public class NetCaching
    {
        private readonly TimeSpan _timeout;
        private readonly MemoryCache _memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        private const string DnsKeyFormat = "{0}:{1}";

        public NetCaching(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        private string GetDnsKey(string hostName, int bindingPort)
        {
            return string.Format(DnsKeyFormat, hostName, bindingPort);
        }

        public async ValueTask<DnsEntry> GetDnsEntryAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal, bool verbatimAddress)
        {
            // See if we already have a cached DnsEntry
            var key = GetDnsKey(hostNameOrAddresss, bindingPort);

            var dnsEntry = _memoryCache.Get<DnsEntry>(key);
            if (dnsEntry != null)
            {
                return dnsEntry;
            }
            else // Else build one, cache it, and return.
            {
                dnsEntry = new DnsEntry
                {
                    HostName = hostNameOrAddresss,
                    Port = bindingPort
                };

                if (overideAsLocal)
                {
                    dnsEntry.PrimaryAddress = IPAddress.Loopback;
                    dnsEntry.Endpoint = new IPEndPoint(IPAddress.Loopback, bindingPort);
                    dnsEntry.Addresses = await Dns.GetHostAddressesAsync(hostNameOrAddresss).ConfigureAwait(false);
                }
                else if (verbatimAddress)
                {
                    dnsEntry.Addresses = await Dns.GetHostAddressesAsync(hostNameOrAddresss).ConfigureAwait(false);

                    // Find verbatim IP address match based on the hostname or address.
                    for (int i = 0; i < dnsEntry.Addresses.Length; i++)
                    {
                        if (dnsEntry.Addresses[i].ToString() == hostNameOrAddresss)
                        {
                            dnsEntry.Endpoint = new IPEndPoint(dnsEntry.Addresses[i], bindingPort);
                            break;
                        }
                    }
                }
                else
                {
                    dnsEntry.Addresses = await Dns.GetHostAddressesAsync(hostNameOrAddresss).ConfigureAwait(false);

                    // Find first non-Loopback address for PrimaryAddress.
                    for (int i = 0; i < dnsEntry.Addresses.Length; i++)
                    {
                        if (!IPAddress.IsLoopback(dnsEntry.Addresses[i]))
                        {
                            dnsEntry.Endpoint = new IPEndPoint(dnsEntry.Addresses[i], bindingPort);
                            break;
                        }
                    }
                }

                _memoryCache.Set(key, dnsEntry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = _timeout });

                return dnsEntry;
            }
        }
    }
}
