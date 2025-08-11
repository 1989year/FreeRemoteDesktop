#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

public static class App
{

    public static string Gateway => "";//wss://xxxxx.tun.pub/worker

    public static Guid Id { get; set; }

    public static string CurrentDirectory { get; } = Path.GetDirectoryName(Environment.ProcessPath);
}