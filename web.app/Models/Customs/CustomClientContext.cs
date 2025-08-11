using System.Net.WebSockets;

public class CustomClientCacheContext
{
    public string Note { get; set; }

    public string Group { get; set; }
}

public class CustomClientContext
{
    public string Group { get; set; } = "默认分组";

    public string Note { get; set; }

    public CustomWebSocket WebSocket { get; set; }

    public CustomOSInformation OSInformation { get; set; }

    public Guid Id { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;
}