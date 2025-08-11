#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型
#pragma warning disable CA1416 // 验证平台兼容性

using RDPCOMAPILib;
using System.Net;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.XPath;

public class WsRdpService : IDisposable
{
    private bool _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true)) {
            return;
        }
        try { _session?.Close(); } catch (Exception) { }
        try {
            while (Marshal.ReleaseComObject(_session) > 0) { }
        } catch (Exception) {
        }
        _session = null;
        Ticket = null;
        LocalIPEndPoints.Clear();
        GC.SuppressFinalize(this);
    }

    public string Ticket { get; private set; }

    private RDPSession _session = null;

    public bool IsActivate { get; private set; } = false;

    public readonly List<IPEndPoint> LocalIPEndPoints = [];

    public void Start(string ticket)
    {
        _session = new RDPSession();
        _session.Properties["EnableClipboardRedirect"] = true;
        _session.Properties["FrameCaptureIntervalInMs"] = 10;
        _session.OnAttendeeConnected += OnAttendeeConnected;
        _session.OnAttendeeDisconnected += OnAttendeeDisconnected;
        _session.OnControlLevelChangeRequest += OnControlLevelChangeRequest;
        _session.Open();
        var invitation = _session.Invitations.CreateInvitation(Guid.NewGuid().ToString("n"), "", "", 32).ConnectionString;
        var xd = XDocument.Parse(invitation);
        xd.XPathSelectElement("/E").SetAttributeValue("TICKET", ticket);
        foreach (var item in xd.Descendants("L")) {
            try {
                if (IPAddress.TryParse(item.Attribute("N").Value, out var addr)
                    && int.TryParse(item.Attribute("P").Value, out var port)) {
                    LocalIPEndPoints.Add(new IPEndPoint(addr, port));
                }
            } catch (Exception) {
            }
        }
        xd.XPathSelectElement("/E/C/T").RemoveNodes();
        Ticket = xd.ToString();
    }

    private void OnAttendeeDisconnected(object pDisconnectInfo)
    {
        IsActivate = false;
    }

    private void OnControlLevelChangeRequest(object pAttendee, CTRL_LEVEL RequestedLevel)
    {
        ((IRDPSRAPIAttendee)pAttendee).ControlLevel = RequestedLevel;
    }

    private void OnAttendeeConnected(object pAttendee)
    {
        IsActivate = true;
        ((IRDPSRAPIAttendee)pAttendee).ControlLevel = CTRL_LEVEL.CTRL_LEVEL_VIEW;
    }
}