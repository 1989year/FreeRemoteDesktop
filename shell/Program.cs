using System.Text.Json;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();

public class Command
{
    public static Command Parse(string text)
    {
        var bytes = Convert.FromBase64String(Uri.UnescapeDataString(text));
        return JsonSerializer.Deserialize<Command>(bytes);
    }

    public string Token { get; set; }

    public string Route { get; set; }

    public string Url { get; set; }
}