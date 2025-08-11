using Custom.WebSocket.Relay;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml.XPath;

namespace desktop;

internal static class App
{
    public readonly static CustomWebSocketRelay Relay = new();

    public readonly static CancellationTokenSource Cts = new();
}

internal static class Program
{
    private readonly static CancellationTokenSource _cts = new();

    private static bool Register()
    {
        try {
            var path = $"\"{Environment.ProcessPath}\" \"%1\"";
            if (!(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ws2\shell\open\command", "", string.Empty)?.Equals(path) ?? false)) {
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ws2", "", "URL:ws2");
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ws2", "URL Protocol", "");
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\ws2\shell\open\command", "", $"\"{Environment.ProcessPath}\" \"%1\"");
            }
            return true;
        } catch (Exception) {
            return false;
        }
    }

    [STAThread]
    static void Main()
    {
        if (!Register()) {
            MessageBox.Show("Failed to register URL Protocol");
            return;
        }

        CustomCommand json;
        try {
            var text = Environment.GetCommandLineArgs()[1].Substring("//", "/");
            json = CustomCommand.Parse(text);
            if (json.Route == "shell") {
                Process.Start("shell.exe", $"--ticket {text}");
                return;
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.ToString());
            return;
        }

        ApplicationConfiguration.Initialize();
        try {
            using var lstnr = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
                NoDelay = true,
                LingerState = new LingerOption(true, 0)
            };
            lstnr.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            string ticket;
            using (HttpClient http = new()) {
                ticket = http.GetStringAsync(json.Url).GetAwaiter().GetResult();
            }
            var xd = XDocument.Parse(ticket);
            XElement tElement = xd.XPathSelectElement("/E/C/T");
            lstnr.Listen();
            tElement.Add(new XElement("L",
                new XAttribute("P", ((IPEndPoint)lstnr.LocalEndPoint).Port),
                new XAttribute("N", "127.0.0.1")));
            var form = new MainForm(xd.XPathSelectElement("/E").Attribute("TICKET").Value, xd.ToString(), json.Token);
            _ = Task.Run(async () => {
                try {
                    var socket = await lstnr.AcceptAsync(App.Cts.Token);
                    await App.Relay.SwapAsync(socket, App.Cts.Token);
                } catch (Exception) {
                } finally {
                    try {
                        form.axrdpViewer1.Invoke(() => form.axrdpViewer1.Disconnect());
                    } catch (Exception) {
                    }
                }
            });
            Application.Run(form);
        } catch (Exception ex) {
            MessageBox.Show(ex.Message);
        } finally {
            _cts.Cancel();
        }
    }
}