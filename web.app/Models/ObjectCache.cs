using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

public class ObjectCache
{
    private readonly IMemoryCache _cache;
    private readonly string _directory;
    private static readonly Lock _lock = new();

    public ObjectCache(IMemoryCache cache)
    {
        _cache = cache;
        _directory = Path.Combine(App.CurrentDirectory, "cache");
        if (!Directory.Exists(_directory)) {
            Directory.CreateDirectory(_directory);
        }
    }

    public void Set(object key, object value)
    {
        lock (_lock) {
            _cache.Set(key, value);
            try {
                File.WriteAllText(Path.Combine(_directory, key.ToString()), JsonSerializer.Serialize(value));
            } catch (Exception) {
            }
        }
    }

    public void Remove(object key)
    {
        lock (_lock) {
            _cache.Remove(key);
            try {
                File.Delete(Path.Combine(_directory, key.ToString()));
            } catch (Exception) {
            }
        }
    }

    public T Get<T>(object key)
    {
        if (_cache.TryGetValue(key, out T value)) {
            return value;
        }
        lock (_lock) {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(_directory, key.ToString())));
        }
    }


    public bool TryGetValue<T>(object key, out T value)
    {
        if (_cache.TryGetValue(key, out value)) {
            return true;
        }
        try {
            lock (_lock) {
                value = JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(_directory, key.ToString())));
                return true;
            }
        } catch (Exception) {
            return false;
        }
    }
}