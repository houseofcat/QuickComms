using QuickComms.Network;
using System;
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
        public ArrayPool<byte> SharedBytePool { get; set; } = ArrayPool<byte>.Shared;

        private Task WriteLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<TSend> MessageChannel { get; }
        public ChannelReader<TSend> MessageChannelReader { get; }
        public ChannelWriter<TSend> MessageChannelWriter { get; }
        public bool LengthWriter { get; }

        public QuickWriter(QuickSocket quickSocket, bool lengthWriter)
        {
            QuickSocket = quickSocket;
            MessageChannel = Channel.CreateUnbounded<TSend>();
            MessageChannelWriter = MessageChannel.Writer;
            MessageChannelReader = MessageChannel.Reader;

            LengthWriter = lengthWriter;
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

        private const byte TerminatingByte = (byte)'\n';
        private const int SequenceLengthSize = 4;

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

                    byte[] payload = null;
                    try
                    {
                        var bytes = JsonSerializer.Serialize(itemToSend);

                        if (LengthWriter)
                        {
                            payload = SharedBytePool.Rent(bytes.Length + SequenceLengthSize);
                            BitConverter.GetBytes(bytes.Length).CopyTo(payload, 0);
                            bytes.CopyTo(payload, SequenceLengthSize);

                            await netStream
                                .WriteAsync(payload, 0, size: bytes.Length + SequenceLengthSize, default).ConfigureAwait(false);
                        }
                        else
                        {
                            payload = SharedBytePool.Rent(bytes.Length + 1);
                            bytes.CopyTo(payload, 0);
                            payload[bytes.Length + 1] = TerminatingByte;

                            await netStream
                                .WriteAsync(payload, 0, size: bytes.Length + 1, default).ConfigureAwait(false);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (payload != null)
                        {
                            SharedBytePool.Return(payload);
                        }
                    }
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
