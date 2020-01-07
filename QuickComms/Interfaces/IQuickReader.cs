using System.Threading.Tasks;

namespace QuickComms
{
    public interface IQuickReader<TReceived>
    {
        Task StartReceiveAsync();
        Task StopReceiveAsync();
    }
}