public class CustomOSInformation
{
    public string CPU { get; set; }

    public string[] GPU { get; set; }

    public string OSDescription { get; set; }

    public string FrameworkDescription { get; set; }

    public string RuntimeIdentifier { get; set; }

    public string MachineName { get; set; }

    public string Version { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;
}