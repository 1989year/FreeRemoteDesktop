#pragma warning disable IDE0079 // ��ɾ������Ҫ�ĺ���
#pragma warning disable CA1050 // �������ռ�����������

public static class App
{

    public static string Gateway => "";//wss://xxxxx.tun.pub/worker

    public static Guid Id { get; set; }

    public static string CurrentDirectory { get; } = Path.GetDirectoryName(Environment.ProcessPath);
}