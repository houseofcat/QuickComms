using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickComms
{
    public interface IFramingStrategy
    {
        bool TryReadSequence(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> sequence);
        Task CreateFrameAndSendAsync(byte[] bytes, NetworkStream netStream);
    }
}