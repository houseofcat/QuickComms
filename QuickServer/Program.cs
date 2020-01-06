using QuickComms;
using QuickComms.Network;
using System;
using System.Threading.Tasks;

namespace QuickServer
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
        private static QuickPipeReader<Message> QuickPipeReader { get; set; }
        private static QuickWriter<MessageReceipt> QuickWriter { get; set; }

        public static async Task Main()
        {
            await SetupServerAsync()
                .ConfigureAwait(false);

            await Console.In.ReadLineAsync().ConfigureAwait(false);
        }

        private static async Task SetupServerAsync()
        {
            await Console.Out.WriteLineAsync("Starting the server connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("127.0.0.1", 5001, true)
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Socket now listening...").ConfigureAwait(false);

            QuickPipeReader = new QuickPipeReader<Message>(quickSocket, true);
            QuickWriter = new QuickWriter<MessageReceipt>(quickSocket, true);

            await QuickPipeReader
                .StartReceiveAsync()
                .ConfigureAwait(false);

            _ = Task.Run(async () =>
            {
                await Console.Out.WriteLineAsync("PipeReader waiting to receive data...").ConfigureAwait(false);

                await foreach (var message in QuickPipeReader.MessageChannelReader.ReadAllAsync())
                {
                    await Console
                        .Out
                        .WriteLineAsync($"MessageId: {message.MessageId}\r\nData: {message.Data}\r\n")
                        .ConfigureAwait(false);
                }
            });
        }
    }
}
