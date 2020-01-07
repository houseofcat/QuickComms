# QuickComms

### General How-To Setup

Here is the basic way to setup an Async Main with RandomBytes and create a Client publishing loop. The Server example is very similar but is setup for subscribing to the socket.

```csharp
private static QuickSocketFactory QuickSocketFactory { get; } = new QuickSocketFactory();
private static QuickUtf8JsonReader<MessageReceipt> QuickJsonReader { get; set; }
private static QuickUtf8JsonWriter<Message> QuickJsonWriter { get; set; }
private static XorShift XorShifter { get; set; }
private static string RandomPayload { get; set; }

public static async Task Main()
{
    XorShifter = new XorShift(true);

    // Create a fixed sized random payload.
    RandomPayload = Encoding.UTF8.GetString(XorShifter.GetRandomBytes(10_000));

    await SetupClientAsync()
        .ConfigureAwait(false);

    await Console.In.ReadLineAsync().ConfigureAwait(false);
}
```

#### Writing Setup Details

Create a QuickSocket by factory, which has a DnsCache, Local Loopback override, port binding, and more.
```csharp
private static async Task SetupClientAsync()
{
    await Console.Out.WriteLineAsync("Starting the client with delay...").ConfigureAwait(false);

    await Task.Delay(2000).ConfigureAwait(false);

    await Console.Out.WriteLineAsync("Starting the client connection now...").ConfigureAwait(false);

    var quickSocket = await QuickSocketFactory
        .GetTcpSocketAsync("127.0.0.1", 15001, true)
        .ConfigureAwait(false);

    var quickListeningSocket = await QuickSocketFactory
        .GetListeningTcpSocketAsync("127.0.0.1", 15001, true)
        .ConfigureAwait(false);
```

Super basic class that provides a function for reading bytes and writing bytes to a stream of your choice.
```csharp
    var framingStrategy = new TerminatedByteFrameStrategy();
```

These classes contain the functionality of reading a specific object and writing a specific object given the appropriate QuickSocket type and byte-based IFramingStrategy. The reader is not used at the moment.
```csharp
    QuickJsonReader = new QuickUtf8JsonReader<MessageReceipt>(quickListeningSocket, framingStrategy);
    QuickJsonWriter = new QuickUtf8JsonWriter<Message>(quickSocket, framingStrategy);
```

Note `Utf8JsonReader` and `Utf8JsonWriter` are in the Extensions library and use Utf8Json for high performance Json serialize and deserialization.

Starts an internal loop that processes a Channel used as a Queue for publishing each object to the socket as bytes
```csharp
    await QuickJsonWriter
        .StartWritingAsync()
        .ConfigureAwait(false);
```

Generating data for the above Writer to use for publishing.
```csharp
    _ = Task.Run(async () =>
    {
        while (true)
        {
            for (int i = 0; i < 5; i++)
            {
                await QuickJsonWriter
                    .QueueForWritingAsync(new Message { MessageId = i, Data = RandomPayload })
                    .ConfigureAwait(false);
            }

            await Task.Delay(1000).ConfigureAwait(false);
        }
    });
}
```

#### Reading Setup Details

Setup QuickSockets for Reading/Writing.
```csharp
private static async Task SetupServerAsync()
{
    await Console.Out.WriteLineAsync("Starting the server connection now...").ConfigureAwait(false);
    
    var quickSocket = await QuickSocketFactory
        .GetTcpSocketAsync("127.0.0.1", 15001, true)
        .ConfigureAwait(false);

    var quickListeningSocket = await QuickSocketFactory
        .GetListeningTcpSocketAsync("127.0.0.1", 15001, true)
        .ConfigureAwait(false);
        
    await Console.Out.WriteLineAsync("Socket now listening...").ConfigureAwait(false);
```

Determine a corresponding framing strategy that the clients should be sending you.

```csharp
    var framingStrategy = new TerminatedByteFrameStrategy();
```

These classes contain the functionality of reading a specific object and writing a specific object given the appropriate QuickSocket type and byte-based IFramingStrategy. The writer is not used at the moment.
```
    QuickJsonReader = new QuickUtf8JsonReader<Message>(quickListeningSocket, framingStrategy);
    QuickJsonWriter = new QuickUtf8JsonWriter<MessageReceipt>(quickSocket, framingStrategy);
```

Starts an internal loop that processes a Socket with a PipeReader to acquire bytes and convert to objects placed in a channel ready for reading.
```csharp
    await QuickJsonReader
        .StartReceiveAsync()
        .ConfigureAwait(false);
```

Loop for reading out of the Channel (code uses the IAsyncEnumerable found on Channels which is available NetCore 3.0+).
```csharp
    _ = Task.Run(async () =>
    {
        await Console.Out.WriteLineAsync("PipeReader waiting to receive data...").ConfigureAwait(false);

        await foreach (var message in QuickJsonReader.MessageChannelReader.ReadAllAsync())
        {
            await Console
                .Out
                .WriteLineAsync($"MessageId: {message.MessageId}\r\nData: {message.Data}\r\n")
                .ConfigureAwait(false);
        }
    });
}
```
