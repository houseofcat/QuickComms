using QuickComms;
using QuickComms.Network;
using System;
using System.Threading.Tasks;

namespace QuickClient
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();

        public static async Task Main(string[] args)
        {
            await Task.Delay(5000).ConfigureAwait(false);
            await Console.Out.WriteLineAsync("Starting the client connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("127.0.0.1", 5001, true)
                .ConfigureAwait(false);

            await quickSocket
                .ConnectToPrimaryAddressAsync()
                .ConfigureAwait(false);

            var quickWriter = new QuickWriter<Message>(quickSocket);
            await quickWriter.StartWritingAsync().ConfigureAwait(false);

            for(int i = 0; i < 5; i ++)
            {
                await quickWriter
                    .QueueForWritingAsync(new Message { MessageId = i, Data = "Hello World" })
                    .ConfigureAwait(false);
            }

            await Console.In.ReadLineAsync();
        }
    }
}
