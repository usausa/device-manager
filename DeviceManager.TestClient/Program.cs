using DeviceManager.Client.Sdk;
using DeviceManager.TestClient;
using Microsoft.Extensions.Logging;

Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  DeviceManager Test Client           ║");
Console.WriteLine("╚══════════════════════════════════════╝");
Console.WriteLine();

var serverUrl = args.Length > 0 ? args[0] : "https://localhost:7125";
var useGrpc = args.Any(a => a.Equals("--grpc", StringComparison.OrdinalIgnoreCase));

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger("TestClient");

var options = new DeviceManagerClientOptions
{
    ServerUrl = serverUrl,
    UseGrpc = useGrpc,
    GrpcUrl = useGrpc ? serverUrl : null,
    AutoReconnect = true,
    DefaultStatusInterval = TimeSpan.FromSeconds(10),
    ConfigCachePath = Path.Combine(Path.GetTempPath(), "dm-test-client", "config-cache.json")
};

var deviceInfo = new TestDeviceInfoProvider();
var statusProvider = new TestStatusProvider();
var commandHandler = new TestCommandHandler();

await using var client = new DeviceManagerClient(
    options, deviceInfo, loggerFactory,
    statusProvider: statusProvider,
    commandHandler: commandHandler);

client.ConnectionStateChanged += (_, state) =>
{
    Console.WriteLine($"  🔗 Connection state: {state}");
};

client.Messages.MessageReceived += (_, msg) =>
{
    Console.WriteLine($"  📨 Message received: [{msg.Type}] {msg.Content}");
};

Console.WriteLine($"  Device ID: {deviceInfo.DeviceId}");
Console.WriteLine($"  Server:    {serverUrl}");
Console.WriteLine($"  Protocol:  {(useGrpc ? "gRPC" : "SignalR")}");
Console.WriteLine();

try
{
    Console.Write("Connecting... ");
    await client.ConnectAsync();
    Console.WriteLine("✅ Connected!");
    Console.WriteLine();

    client.StartStatusReporting(TimeSpan.FromSeconds(10));

    Console.WriteLine("Commands:");
    Console.WriteLine("  [m] Send message   [d] Set data store   [c] Get config");
    Console.WriteLine("  [s] Send status    [q] Quit");
    Console.WriteLine();

    while (true)
    {
        Console.Write("> ");
        var key = Console.ReadKey(true);
        Console.WriteLine(key.KeyChar);

        switch (key.KeyChar)
        {
            case 'q':
            case 'Q':
                Console.WriteLine("Disconnecting...");
                await client.DisconnectAsync();
                Console.WriteLine("Bye!");
                return;

            case 'm':
                Console.Write("  Message type: ");
                var msgType = Console.ReadLine() ?? "test.message";
                Console.Write("  Content: ");
                var msgContent = Console.ReadLine() ?? "Hello from test client";
                await client.Messages.SendAsync(msgType, msgContent);
                Console.WriteLine("  ✅ Message sent");
                break;

            case 'd':
                Console.Write("  Key: ");
                var dataKey = Console.ReadLine() ?? "test-key";
                Console.Write("  Value: ");
                var dataValue = Console.ReadLine() ?? "test-value";
                await client.DataStore.SetAsync(dataKey, dataValue);
                Console.WriteLine("  ✅ Data stored");
                break;

            case 'c':
                var config = await client.Config.GetAllAsync();
                if (config.Count == 0)
                {
                    Console.WriteLine("  (no config entries)");
                }
                else
                {
                    foreach (var entry in config)
                    {
                        Console.WriteLine($"  {entry.Key} = {entry.Value} [{entry.ValueType}]");
                    }
                }
                break;

            case 's':
                Console.WriteLine("  Sending manual status report...");
                var status = await statusProvider.GetCurrentStatusAsync();
                Console.WriteLine($"  Level={status.Level}, Progress={status.Progress:F1}%, Battery={status.Battery}%");
                break;
        }
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Test client error");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
