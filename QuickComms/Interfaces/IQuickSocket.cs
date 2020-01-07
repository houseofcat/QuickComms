using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickComms
{
    public interface IQuickSocket
    {
        Socket Socket { get; }
        Task ConnectToPrimaryAddressAsync();
        Task ConnectToAddressesAsync();
        Task ShutdownAsync();
    }
}
