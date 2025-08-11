#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using System.Reflection;
using System.Runtime.InteropServices;

public class CustomOSInformation
{
    public string CPU { get; set; } = Hwid.CPU;

    public string[] GPU { get; set; } = Hwid.GPU;

    public string OSDescription { get; set; } = RuntimeInformation.OSDescription;

    public string FrameworkDescription { get; set; } = RuntimeInformation.FrameworkDescription;

    public string RuntimeIdentifier { get; set; } = RuntimeInformation.RuntimeIdentifier;

    public string MachineName { get; set; } = Environment.MachineName;

    public string Version { get; set; } = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0.0";
}