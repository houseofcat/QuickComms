using System.Threading.Tasks;

namespace QuickComms
{
    public interface IQuickSocketFactory
    {
        ValueTask<IQuickSocket> GetTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
        ValueTask<IQuickSocket> GetUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
        ValueTask<IQuickListeningSocket> GetListeningTcpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
        ValueTask<IQuickListeningSocket> GetListeningUdpSocketAsync(string hostNameOrAddresss, int bindingPort, bool overideAsLocal = false, bool verbatimAddress = false);
    }
}
