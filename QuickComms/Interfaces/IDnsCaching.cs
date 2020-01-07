using QuickComms.Network;
using System.Threading.Tasks;

namespace QuickComms
{
    public interface IDnsCaching
    {
        ValueTask<DnsEntry> GetDnsEntryAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal, bool verbatimAddress);
        void RemoveDnsEntry(string hostNameOrAddresss, int bindingPort);
    }
}