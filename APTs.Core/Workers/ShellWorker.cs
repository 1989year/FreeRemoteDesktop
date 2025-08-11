#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型
using Custom.WebSocket.Relay;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;

public class ShellWorker(IHostApplicationLifetime app, IConfiguration cfg) : BackgroundService
{
    private readonly Lock _lock = new();
    private readonly CustomWebSocketRelay _websocket = new();
    private bool _isFirstLine = true;
    private StreamWriter _inputWriter = null;
    private Process _prc = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try {
            var pipeName = cfg.GetValue<string>("pipe");
            ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
            var ticket = await _websocket.ConnectAsync(App.Gateway, true, stoppingToken);
            using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out)) {
                await pipe.ConnectAsync(3000, stoppingToken);
                await pipe.WriteAsync(Encoding.UTF8.GetBytes(ticket), stoppingToken);
                await pipe.FlushAsync(stoppingToken);
            }
            await _websocket.WaitAsync(1000 * 30, stoppingToken);
            await _websocket.ReceiveAsync(async (data) => {
                var cmd = Encoding.UTF8.GetString(data.Span);
                ArgumentOutOfRangeException.ThrowIfEqual(ExecuteCommand(cmd), false);
                await Task.CompletedTask;
            }, stoppingToken);
        } finally {
            app.StopApplication();
        }
    }

    private bool ExecuteCommand(string command)
    {
        lock (_lock) {
            if (_prc == null || _prc.HasExited) {
                try {
                    CreateSession();
                } catch (Exception) {
                    return false;
                }
            }
            _inputWriter.WriteLine(command);
            _inputWriter.Flush();
            _inputWriter.WriteLine(string.Empty);
            _inputWriter.Flush();
            return true;
        }
    }

    private void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) {
            return;
        }
        lock (_lock) {
            if (_isFirstLine) {
                _isFirstLine = false;
            } else {
                if (e.Data.EndsWith('>')) {
                    _isFirstLine = true;
                }
                try {
                    _websocket.SendAsync(e.Data, default).GetAwaiter().GetResult();
                } catch (Exception) {
                    app.StopApplication();
                }
            }
        }
    }

    private void CreateSession()
    {
        lock (_lock) {
            try {
                _prc?.Kill();
            } catch (Exception) {
                try {
                    _prc?.Dispose();
                } catch (Exception) {
                }
            }
            _prc = new Process {
                StartInfo = new ProcessStartInfo("cmd") {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System),
                }
            };
            _prc.OutputDataReceived += OutputDataReceived;
            _prc.ErrorDataReceived += OutputDataReceived;
            _prc.Start();
            _prc.BeginOutputReadLine();
            _prc.BeginErrorReadLine();
            _inputWriter = new StreamWriter(_prc.StandardInput.BaseStream);
        }
    }
}