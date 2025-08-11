#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace System.Net.WebSockets;

public sealed class WebSocketFactory(ClientWebSocket websocket, byte[] key) : IDisposable
{
    private readonly CustomAesGcm _aes = new(key);
    private readonly ArrayBufferWriter<byte> _buffer = new();
    private bool _disposed;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true)) {
            return;
        }
        websocket.Dispose();
        _buffer.Clear();
        _aes.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task ReceiveAsync(Func<int, ReadOnlyMemory<byte>, Task> callback, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        while (!stoppingToken.IsCancellationRequested) {
            var result = await websocket.ReceiveAsync(owner.Memory, stoppingToken).ConfigureAwait(false);
            _buffer.Write(owner.Memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    using var packet = _aes.Decode(_buffer.WrittenSpan, out var length);
                    var memory = packet.Memory[..length];
                    var action = MemoryMarshal.Read<int>(memory.Span[..4]);
                    length = MemoryMarshal.Read<int>(memory.Span.Slice(4, 4));
                    await callback(action, length == 0 ? ReadOnlyMemory<byte>.Empty : memory.Slice(8, length)).ConfigureAwait(false);
                } catch (Exception) {
                } finally {
                    _buffer.Clear();
                }
            }
        }
    }

    public ValueTask SendAsync(int action, string s, CancellationToken cancellationToken)
    {
        return this.SendAsync(action, Encoding.UTF8.GetBytes(s), cancellationToken);
    }

    public ValueTask SendAsync(int action, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        var length = 8 + bytes.Length;
        using var owner = MemoryPool<byte>.Shared.Rent(length);
        var buffer = owner.Memory[..length];
        MemoryMarshal.Write(buffer.Span[..4], action);
        MemoryMarshal.Write(buffer.Span[4..], bytes.Length);
        if (bytes.Length > 0) {
            bytes.Span.CopyTo(buffer.Span[8..]);
        }
        using var ciphertext = _aes.Encode(buffer.Span, out length);
        return websocket.SendAsync(ciphertext.Memory[..length], WebSocketMessageType.Binary, true, cancellationToken);
    }

    public async Task NativeReceiveAsync(Func<ReadOnlyMemory<byte>, ValueTask<int>> func, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var buffer = owner.Memory;
        while (!stoppingToken.IsCancellationRequested) {
            var result = await websocket.ReceiveAsync(buffer, stoppingToken).ConfigureAwait(false);
            _buffer.Write(buffer.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    using var plaintext = _aes.Decode(_buffer.WrittenSpan, out int length);
                    await func(plaintext.Memory.Slice(0, length)).ConfigureAwait(false);
                } finally {
                    _buffer.Clear();
                }
            }
        }
    }

    public ValueTask NativeSendAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        using var ciphertext = _aes.Encode(bytes.Span, out var length);
        return websocket.SendAsync(ciphertext.Memory[..length], WebSocketMessageType.Binary, true, cancellationToken);
    }

    public static async Task CreateAsync(string url, Func<WebSocketFactory, Task> callback, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(6000);
        var key = RandomNumberGenerator.GetBytes(32);
        using var websocket = new ClientWebSocket();
        websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        websocket.Options.KeepAliveTimeout = TimeSpan.FromSeconds(60);
        websocket.Options.SetRequestHeader("aes-key", Convert.ToBase64String(key));
        await websocket.ConnectAsync(new Uri(url), cts.Token);
        using var ctx = new WebSocketFactory(websocket, key);
        await callback(ctx);
    }
}