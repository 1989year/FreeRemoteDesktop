using Custom.WebSocket.Relay;
using System.Text;

public class Worker(ILogger<Worker> logger, IHostApplicationLifetime lifetime, IConfiguration cfg) : BackgroundService
{
    public readonly CustomWebSocketRelay _websocket = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var json = Command.Parse(cfg.GetValue<string>("ticket"));
        Console.Title = json.Token;
        logger.LogWarning("{s}", "Connecting to remote shell");
        await _websocket.ConnectAsync(json.Url, false, stoppingToken);
        try {
            Console.Clear();
            _ = Task.Run(async () => {
                int i = 0;
                await _websocket.SendAsync("hostname", stoppingToken);
                while (!stoppingToken.IsCancellationRequested) {
                    i++;
                    var cmd = Console.ReadLine();
                    await _websocket.SendAsync(cmd, stoppingToken);
                }
            });
            await _websocket.ReceiveAsync(async (ReadOnlyMemory<byte> data) => {
                var str = Encoding.UTF8.GetString(data.Span);
                if (str.EndsWith('>')) {
                    Console.Write(str);
                } else {
                    Console.WriteLine(str);
                }
                await Task.CompletedTask;
            }, stoppingToken);
        } finally {
            lifetime.StopApplication();
        }
    }
}