using QuickComms;
using QuickComms.Network;
using System;
using System.Threading.Tasks;

namespace QuickClient
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
        private static QuickPipeReader<MessageReceipt> QuickPipeReader { get; set; }
        private static QuickWriter<Message> QuickWriter { get; set; }

        public static async Task Main()
        {
            await SetupClientAsync()
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task SetupClientAsync()
        {
            await Console.Out.WriteLineAsync("Starting the client with delay...").ConfigureAwait(false);

            await Task.Delay(5000).ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Starting the client connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("127.0.0.1", 5001, true)
                .ConfigureAwait(false);

            QuickPipeReader = new QuickPipeReader<MessageReceipt>(quickSocket);
            QuickWriter = new QuickWriter<Message>(quickSocket);

            await QuickWriter
                .StartWritingAsync()
                .ConfigureAwait(false);

            // Publish To Server
            _ = Task.Run(async () =>
            {
                for (int i = 0; i < 10_000; i++)
                {
                    var writeTask1 = QuickWriter
                        .QueueForWritingAsync(new Message { MessageId = i++, Data = "Hello World1" });

                    var writeTask2 = QuickWriter
                        .QueueForWritingAsync(new Message { MessageId = i, Data = "Hello World2" });

                    await Task.WhenAll(writeTask1, writeTask2).ConfigureAwait(false);
                }
            });
        }
    }
}
