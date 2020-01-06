using QuickComms.Network;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;

namespace QuickComms
{
    public class QuickWriter<TSend>
    {
        public NetworkStream NetStream { get; }
        public bool Write { get; private set; }

        private Task WriteLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<TSend> MessageChannel { get; }
        public ChannelReader<TSend> MessageChannelReader { get; }
        public ChannelWriter<TSend> MessageChannelWriter { get; }

        public QuickWriter(QuickSocket quickSocket)
        {
            NetStream = new NetworkStream(quickSocket.Socket);
            MessageChannel = Channel.CreateUnbounded<TSend>();
            MessageChannelWriter = MessageChannel.Writer;
            MessageChannelReader = MessageChannel.Reader;
        }

        public async Task QueueForWritingAsync(TSend obj)
        {
            if (await MessageChannelWriter.WaitToWriteAsync().ConfigureAwait(false))
            {
                await MessageChannelWriter
                    .WriteAsync(obj)
                    .ConfigureAwait(false);
            }
        }

        public async Task StartWritingAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (!Write)
            { Write = true; }

            WriteLoopTask = Task.Run(WriteAsync);

            PipeLock.Release();
        }

        private async Task WriteAsync()
        {
            while (Write)
            {
                if (await MessageChannelReader.WaitToReadAsync().ConfigureAwait(false))
                {
                    var itemToSend = await MessageChannelReader
                        .ReadAsync()
                        .ConfigureAwait(false);

                    await NetStream.WriteAsync(JsonSerializer.Serialize(itemToSend));
                }
            }

            await StopWriteAsync().ConfigureAwait(false);
        }

        public async Task StopWriteAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (Write)
            { Write = false; }

            PipeLock.Release();
        }
    }
}
