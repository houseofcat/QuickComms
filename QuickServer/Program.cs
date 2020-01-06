using QuickComms;
using QuickComms.Network;
using System;
using System.Threading.Tasks;

namespace QuickServer
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();

        public static async Task Main(string[] args)
        {
            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("127.0.0.1", 5001, true)
                .ConfigureAwait(false);

            await quickSocket
                .ConnectToPrimaryAddressAsync(true)
                .ConfigureAwait(false);

            var quickPipeReader = new QuickPipeReader<Message>(quickSocket);

            await quickPipeReader
                .StartReceiveAsync()
                .ConfigureAwait(false);

            await foreach(var message in quickPipeReader.MessageChannelReader.ReadAllAsync())
            {
                await Console
                    .Out
                    .WriteLineAsync($"MessageId: {message.MessageId}\r\nData:{message.Data}\r\n")
                    .ConfigureAwait(false);
            }
        }
    }
}
