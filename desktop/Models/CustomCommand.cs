using System.Text.Json;

public class CustomCommand
{
    public static CustomCommand Parse(string text)
    {
        return JsonSerializer.Deserialize<CustomCommand>(Convert.FromBase64String(Uri.UnescapeDataString(text)));
    }

    public string Token { get; set; }

    public string Route { get; set; }

    public string Url { get; set; }
}