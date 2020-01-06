using QuickComms.Network;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;

namespace QuickComms
{
    public class QuickWriter<TSend>
    {
        public QuickSocket QuickSocket { get; }
        public bool Write { get; private set; }

        private Task WriteLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<TSend> MessageChannel { get; }
        public ChannelReader<TSend> MessageChannelReader { get; }
        public ChannelWriter<TSend> MessageChannelWriter { get; }

        public QuickWriter(QuickSocket quickSocket)
        {
            QuickSocket = quickSocket;
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

            await QuickSocket
                .ConnectToPrimaryAddressAsync()
                .ConfigureAwait(false);

            WriteLoopTask = Task.Run(WriteAsync);

            PipeLock.Release();
        }

        private async Task WriteAsync()
        {
            using var netStream = new NetworkStream(QuickSocket.Socket);

            while (Write)
            {
                if (await MessageChannelReader.WaitToReadAsync().ConfigureAwait(false))
                {
                    var itemToSend = await MessageChannelReader
                        .ReadAsync()
                        .ConfigureAwait(false);

                    await netStream
                        .WriteAsync(CreatePayload(itemToSend));
                }
            }

            await StopWriteAsync().ConfigureAwait(false);
        }

        private const byte TerminatingByte = (byte)'\n';

        private byte[] CreatePayload(TSend itemToSend)
        {
            var bytes = JsonSerializer.Serialize(itemToSend);
            byte[] payload = null;

            try
            {
                payload = ArrayPool<byte>.Shared.Rent(bytes.Length + 1);
                bytes.CopyTo(payload, 0);
                payload[^1] = TerminatingByte;

                return payload;
            }
            finally
            {
                if (payload != null)
                {
                    ArrayPool<byte>.Shared.Return(payload);
                }
            }
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
