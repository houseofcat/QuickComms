using QuickComms;
using QuickComms.Network;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace QuickServer
{
    public static class Program
    {
        private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
        private static Task AcceptConnectionsTask { get; set; }

        public static async Task Main(string[] args)
        {
            await Console.Out.WriteLineAsync("Starting the server connection now...").ConfigureAwait(false);

            var quickSocket = await QuickSocketFactory
                .GetTcpStreamSocketAsync("127.0.0.1", 5001, true)
                .ConfigureAwait(false);

            await quickSocket
                .BindSocketToAddressAsync(100)
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Socket now listening...").ConfigureAwait(false);

            var quickPipeReader = new QuickPipeReader<Message>(quickSocket);

            await quickPipeReader
                .StartReceiveAsync()
                .ConfigureAwait(false);

            await Console.Out.WriteLineAsync("PipeReader waiting to receive data...").ConfigureAwait(false);

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
