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
            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("192.168.50.176", 5001, false, true)
                .ConfigureAwait(false);

            await quickSocket
                .ConnectToPrimaryAddressAsync(false)
                .ConfigureAwait(false);

            var quickWriter = new QuickWriter<Message>(quickSocket);
            await quickWriter.StartWritingAsync().ConfigureAwait(false);

            for(int i = 0; i < 5; i ++)
            {
                await quickWriter
                    .QueueForWritingAsync(new Message { MessageId = i, Data = "Hello World" })
                    .ConfigureAwait(false);
            }
        }
    }
}
