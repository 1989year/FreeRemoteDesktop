using AxRDPCOMAPILib;
using RDPCOMAPILib;

namespace desktop;

public partial class MainForm : Form
{
    private readonly string _url;
    private readonly string _ticket;

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == 0x112) {
            SystemMenuEvent(m);
        }
        void SystemMenuEvent(Message m)
        {
            IntPtr hwnd;
            try {
                hwnd = User32.GetSystemMenu(this.Handle, false);
                ArgumentOutOfRangeException.ThrowIfEqual(hwnd, IntPtr.Zero);
            } catch (Exception) {
                return;
            }
            bool state;
            switch (m.WParam & 0xFFFF) {
                case 1000:
                    break;
                case 1001:
                    try {
                        state = this.GetSystemMenuCheckState(1001);
                        state = this.SetSystemMenuCheckState(1001, !state);
                        axrdpViewer1.RequestControl(state ? CTRL_LEVEL.CTRL_LEVEL_INTERACTIVE : CTRL_LEVEL.CTRL_LEVEL_VIEW);
                    } catch (Exception) {
                        state = this.GetSystemMenuCheckState(1001);
                        state = this.SetSystemMenuCheckState(1001, !state);
                    }
                    break;
                case 1002:
                    break;
                case 1003:
                    break;
                case 1025:
                    PMQ.Start(250, 250);
                    break;
                case 1030:
                    PMQ.Start(300, 300);
                    break;
                case 1035:
                    PMQ.Start(350, 350);
                    break;
                case 1040:
                    PMQ.Start(400, 400);
                    break;
            }
        }
    }

    public MainForm(string url, string ticket, string token)
    {
        _url = url;
        _ticket = ticket;
        InitializeComponent();
        Text = token;
        axrdpViewer1.Dock = DockStyle.Fill;
        axrdpViewer1.SmartSizing = true;
        axrdpViewer1.OnConnectionTerminated += OnConnectionTerminated;
        axrdpViewer1.OnConnectionEstablished += OnConnectionEstablished;
    }

    private void OnConnectionTerminated(object sender, _IRDPSessionEvents_OnConnectionTerminatedEvent e)
    {
        try {
            lbMgr.Show();
            lbMgr.Text = $"Óë¿Í»§¶ËµÄÁ¬½ÓÒÑ¶Ï¿ª [ {DateTime.Now} ]";
        } catch (Exception) {
        }
    }

    private async void OnConnectionEstablished(object sender, EventArgs e)
    {
        try {
            pbLoading.Hide();
            await Task.Delay(100);
            axrdpViewer1.Show();
            this.AddSystemMenu();
            this.AddSystemMenu(0x00000400, 1025, "ÆÁÄ»Ç½ 250x250");
            this.AddSystemMenu(0x00000400, 1030, "ÆÁÄ»Ç½ 300x300");
            this.AddSystemMenu(0x00000400, 1035, "ÆÁÄ»Ç½ 350x350");
            this.AddSystemMenu(0x00000400, 1040, "ÆÁÄ»Ç½ 400x400");
            this.AddSystemMenu();
            this.AddSystemMenu(0x00000400, 1001, "¿ØÖÆ");
        } catch (Exception) {
        }
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        MainForm_Resize(sender, e);
        _ = Task.Run(async () => {
            while (true) {
                IntPtr notepadHwnd = User32.FindWindow(null, _ticket);
                try {
                    if (notepadHwnd != IntPtr.Zero) {
                        User32.ShowWindow(notepadHwnd, 9);
                        User32.BringWindowToTop(notepadHwnd);
                        User32.SetForegroundWindow(notepadHwnd);
                        break;
                    }
                } finally {
                    await Task.Delay(100);
                }
            }
        });
        try {
            await App.Relay.ConnectAsync(_url, false, App.Cts.Token);
            axrdpViewer1.Connect(_ticket, string.Empty, "");
        } catch (Exception) {
        }
    }

    private void MainForm_Resize(object sender, EventArgs e)
    {
        try {
            if (WindowState != FormWindowState.Minimized) {
                pbLoading.SetCenterLocation(this);
            }
        } catch (Exception) {
        }
    }
}