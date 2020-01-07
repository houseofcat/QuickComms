using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickComms
{
    public interface IQuickListeningSocket
    {
        Socket Socket { get; }
        Task BindSocketToAddressAsync(int pendingConnections);
        Task ShutdownAsync();
    }
}
