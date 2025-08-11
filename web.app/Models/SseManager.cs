using System.Collections.Concurrent;
using System.Text.Json;

public class SseManager
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly ConcurrentDictionary<string, (StreamWriter Stream, CancellationTokenSource Cts)> _lists = [];

    public async Task PublishAsync(params object[] args)
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try {
            var text = $"data: {JsonSerializer.Serialize(args)}\n\n";
            foreach (var item in _lists.Values.ToArray()) {
                try {
                    if (item.Cts.IsCancellationRequested) {
                        continue;
                    }
                    await item.Stream.WriteLineAsync(text).ConfigureAwait(false);
                    await item.Stream.FlushAsync(item.Cts.Token).ConfigureAwait(false);
                } catch (Exception) {
                }
            }
        } catch (Exception) {
        } finally {
            _lock.Release();
        }
    }

    public async Task<bool> TryAddAsync(string id, StreamWriter stream, CancellationTokenSource cts)
    {
        while (_lists.TryGetValue(id, out var obj)) {
            try {
                obj.Cts.Cancel();
            } catch (Exception) {
            }
            await Task.Delay(1000, cts.Token).ConfigureAwait(false);
        }
        return _lists.TryAdd(id, (stream, cts));
    }

    public void Remove(string id)
    {
        if (_lists.TryRemove(id, out var obj)) {
            try {
                obj.Cts.Cancel();
            } catch (Exception) {
            }
        }
    }
}