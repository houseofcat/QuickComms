using System.Threading.Tasks;

namespace QuickComms
{
    public interface IQuickWriter<TSend>
    {
        Task QueueForWritingAsync(TSend obj);
        Task StartWritingAsync();
        Task StopWriteAsync();
    }
}