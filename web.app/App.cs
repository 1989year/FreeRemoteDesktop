#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using System.Text.Json;

public static class App
{
    public readonly static JsonSerializerOptions _json_serializer_options = new() {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string CurrentDirectory { get; } = Path.GetDirectoryName(Environment.ProcessPath);

    public const string PUBLIC_KEY = "-----BEGIN RSA PUBLIC KEY-----\r\nMIIBCgKCAQEAtbEUm+qvv44VIGYoTFOE2m7vByDcWqfMCwmPXMtrhGp3/zFtUgm0\r\nmVyg4ntmI2m4uwz2DD3p244ks2XQUH1igk6MsBbFBJpdMkU9hwq3KYoL4RLNkXN5\r\nyfkYmIST6LuxUStlBTD4d3qIFKo9kMoZ4WiwNjZrGTFAz4rxwz4BxuqjiKulymbd\r\nLVa8UtWyrVOPoRRDo/gw+EHCyXehDEU+fjW8IOcr3TgFuNC80RAIN0Z2ECHTex84\r\nhGoT63zbWQXgwgdJa2+hrwEFqUUtH6JBPROqnrLeEwuwMYNu1X2ugVzRxUK/jke4\r\n1ZPYUsIVSeD5uTE4RXrT2jpCTVWxHncz9QIDAQAB\r\n-----END RSA PUBLIC KEY-----";
}