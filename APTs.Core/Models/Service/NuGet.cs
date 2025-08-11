#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using System.IO.Compression;

public class NuGet(IHttpClientFactory _http)
{
    public async Task DownloadAsync(string fileName, string version)
    {
        var nupkg = Path.Combine(App.CurrentDirectory, $"{Path.GetFileNameWithoutExtension(fileName)}.{version}.nupkg");
        if (!File.Exists(nupkg)) {
            using var http = _http.CreateClient();
            var bytes = await http.GetByteArrayAsync($"https://globalcdn.nuget.org/packages/{Path.GetFileName(nupkg)}");
            await File.WriteAllBytesAsync(nupkg, bytes);
        }
        try {
            using var stream = File.OpenRead(nupkg);
            using var zf = new ZipArchive(stream, ZipArchiveMode.Read);
            zf.Entries.Single(entry => entry.FullName.Contains($"runtimes/win/lib/net{Environment.Version.ToString(2)}/{fileName}",
                StringComparison.OrdinalIgnoreCase)).ExtractToFile(Path.Combine(App.CurrentDirectory, fileName), true);
        } catch (Exception) {
            File.Delete(nupkg);
        }
    }
}