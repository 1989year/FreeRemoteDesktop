#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using Custom.WebSocket.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.Loader;

public class Program : AssemblyLoadContext
{
    public Program(CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder(Environment.GetCommandLineArgs());
        //builder.Logging.AddFilter("Microsoft.Hosting.*", LogLevel.None);
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<NuGet>();
        builder.Services.AddTransient<CustomWebSocketRelay>();
        builder.Services.AddTransient<WsRdpService>();
        if (builder.Configuration.GetValue("pipe", Guid.Empty) == Guid.Empty) {
            builder.Services.AddHostedService<MainWorker>();
        } else {
            switch (builder.Configuration.GetValue("type", string.Empty)) {
                case "desktop":
                    builder.Services.AddHostedService<DesktopWorker>();
                    break;
                case "shell":
                    builder.Services.AddHostedService<ShellWorker>();
                    break;
                default: throw new NotSupportedException();
            }
        }
        try {
            builder.Build().RunAsync(cancellationToken).GetAwaiter().GetResult();
        } catch (OperationCanceledException) {
        }
    }
}