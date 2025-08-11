#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
#pragma warning disable CA1416 // 验证平台兼容性
using RDPCOMAPILib;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml.XPath;

public class RdpServiceV2
{
    private CancellationTokenSource _cts = null;

    public CancellationToken CancellationToken { get => _cts.Token; }

    public string Ticket { get; private set; }

    private RDPSession _session = null;

    public bool IsActivate { get; private set; } = false;

    public readonly List<IPEndPoint> LocalIPEndPoints = [];

    public async Task StartAsync(Guid id, CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try {
            _session = new RDPSession();
            _session.Properties["EnableClipboardRedirect"] = true;
            _session.Properties["FrameCaptureIntervalInMs"] = 10;
            _session.OnAttendeeConnected += OnAttendeeConnected;
            _session.OnAttendeeDisconnected += OnAttendeeDisconnected;
            _session.OnControlLevelChangeRequest += OnControlLevelChangeRequest;
            _session.Open();
            var ticket = _session.Invitations.CreateInvitation(Guid.NewGuid().ToString("n"), "", "", 32).ConnectionString;
            _cts.Token.Register(() => _session.Close());
            var xd = XDocument.Parse(ticket);

            // Set the ID attribute in the root element
            xd.XPathSelectElement("/E").SetAttributeValue("RELAYID", id.ToString());

            // element for the host and port
            foreach (var item in xd.Descendants("L")) {
                try {
                    if (IPAddress.TryParse(item.Attribute("N").Value, out var addr)
                        && int.TryParse(item.Attribute("P").Value, out var port)) {
                        LocalIPEndPoints.Add(new IPEndPoint(addr, port));
                    }
                } catch (Exception) {
                }
            }
            // 移除所有监听信息
            xd.XPathSelectElement("/E/C/T").RemoveNodes();
            this.Ticket = xd.ToString();
            await Task.CompletedTask;
        } catch (Exception) {
            _cts.Dispose();
            throw;
        }
    }

    private void OnAttendeeDisconnected(object pDisconnectInfo)
    {
        _cts.Cancel();
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