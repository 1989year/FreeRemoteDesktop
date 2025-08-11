#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
#pragma warning disable CA1416 // 验证平台兼容性
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

internal partial class Utils
{
    public static async Task<CustomEndPoint> PingsAsync(CustomEndPoint[] array, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(array.Length);
        var results = new ConcurrentBag<(CustomEndPoint EndPoint, long Time)>();
        var options = new ParallelOptions {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10
        };
        await Parallel.ForEachAsync(array, options, async (item, ct) => {
            using (var ping = new Ping()) {
                try {
                    var reply = await ping.SendPingAsync(item.Host, TimeSpan.FromSeconds(3), new byte[32], null, ct).ConfigureAwait(false);
                    if (reply.Status == IPStatus.Success) {
                        results.Add((item, reply.RoundtripTime));
                    }
                } catch (Exception) {
                }
            }
        }).ConfigureAwait(false);
        return results.OrderBy(x => x.Time).Select(x => x.EndPoint).First();
    }
}