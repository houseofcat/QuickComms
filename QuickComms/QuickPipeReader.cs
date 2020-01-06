using QuickComms.Network;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;

namespace QuickComms
{
    public class QuickPipeReader<TReceived>
    {
        public NetworkStream NetStream { get; }
        public PipeReader PipeReader { get; }
        public bool Receive { get; private set; }

        private Task ReceiveLoopTask { get; set; }
        private SemaphoreSlim PipeLock { get; } = new SemaphoreSlim(1, 1);
        private Channel<TReceived> MessageChannel { get; }
        public ChannelReader<TReceived> MessageChannelReader { get; }

        public QuickPipeReader(QuickSocket quickSocket)
        {
            NetStream = new NetworkStream(quickSocket.Socket);
            PipeReader = PipeReader.Create(NetStream);
            MessageChannel = Channel.CreateUnbounded<TReceived>();
            MessageChannelReader = MessageChannel.Reader;
        }

        public QuickPipeReader(Socket socket)
        {
            NetStream = new NetworkStream(socket);
            PipeReader = PipeReader.Create(NetStream);
            MessageChannel = Channel.CreateUnbounded<TReceived>();
            MessageChannelReader = MessageChannel.Reader;
        }

        public QuickPipeReader(NetworkStream netStream)
        {
            NetStream = netStream;
            PipeReader = PipeReader.Create(netStream);
            MessageChannel = Channel.CreateUnbounded<TReceived>();
            MessageChannelReader = MessageChannel.Reader;
        }

        public QuickPipeReader(Pipe pipe)
        {
            PipeReader = pipe.Reader;
            NetStream = (NetworkStream)pipe.Reader.AsStream();
            PipeLock = new SemaphoreSlim(1, 1);
            MessageChannel = Channel.CreateUnbounded<TReceived>();
            MessageChannelReader = MessageChannel.Reader;
        }

        public async Task StartReceiveAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (!Receive)
            { Receive = true; }

            ReceiveLoopTask = Task.Run(ReceiveAsync);

            PipeLock.Release();
        }

        private async Task ReceiveAsync()
        {
            while (Receive)
            {
                ReadResult result = await PipeReader.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = result.Buffer;

                if (result.IsCanceled)
                { break; }

                // Trying to find all full sequences in the current buffer.
                while (TryReadSequence(ref buffer, out ReadOnlySequence<byte> line))
                {
                    await MessageChannel
                        .Writer
                        .WriteAsync(JsonSerializer.Deserialize<TReceived>(line.ToArray()))
                        .ConfigureAwait(false);
                }

                // Buffer position was modified in TryReadSequence to include exact amounts consumed and read (if any).
                PipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                { break; }
            }

            await StopReceiveAsync().ConfigureAwait(false);
        }

        public async Task StopReceiveAsync()
        {
            await PipeLock.WaitAsync().ConfigureAwait(false);

            if (Receive)
            { Receive = false; }

            PipeLock.Release();
        }

        private const byte TerminatingByte = (byte)'\n';

        private bool TryReadSequence(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            SequencePosition? position = buffer.PositionOf(TerminatingByte);

            // If terminating character is not found, exit false.
            if (position == null)
            {
                line = default;
                return false;
            }

            // Get a readonly sequence upto the next line terminator but not including the last one.
            line = buffer.Slice(0, position.Value);
            buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            return true;
        }
    }
}
