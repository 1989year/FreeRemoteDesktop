#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型
using Custom.WebSocket.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO.Pipes;
using System.Text;

public class DesktopWorker(ILogger<DesktopWorker> logger, CustomWebSocketRelay relay, WsRdpService rdpsvc, IHostApplicationLifetime lifetime, IConfiguration cfg) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try {
            var ticket = await relay.ConnectAsync(App.Gateway, true, stoppingToken);
            rdpsvc.Start(ticket);
            using (var pipe = new NamedPipeClientStream(".", cfg.GetValue<string>("pipe"), PipeDirection.Out)) {
                await pipe.ConnectAsync(3000, stoppingToken);
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(rdpsvc.Ticket), stoppingToken);
                await pipe.FlushAsync(stoppingToken);
            }
            await relay.WaitAsync(1000 * 30, stoppingToken);
            foreach (var item in rdpsvc.LocalIPEndPoints) {
                try {
                    await relay.SwapAsync(item.Address, item.Port, stoppingToken);
                    break;
                } catch (Exception) {
                }
            }
        } catch (Exception ex) {
            logger.LogError("{ex}", ex);
        } finally {
            lifetime.StopApplication();
        }
    }
}