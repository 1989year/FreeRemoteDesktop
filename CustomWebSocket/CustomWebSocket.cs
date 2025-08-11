#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

using System.Buffers;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets;

public sealed class CustomWebSocket(WebSocket websocket, ReadOnlySpan<byte> key) : IDisposable
{
    private readonly CustomAesGcm _aes = new(key);
    private readonly ArrayBufferWriter<byte> _buffer = new();
    private bool _disposed;

    public async Task ReceiveAsync(Func<int, ReadOnlyMemory<byte>, Task> callback, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var memory = owner.Memory;
        while (!stoppingToken.IsCancellationRequested) {
            var result = await websocket.ReceiveAsync(memory, stoppingToken).ConfigureAwait(false);
            _buffer.Write(memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    using var plaintext = _aes.Decode(_buffer.WrittenSpan, out var length);
                    var packet = plaintext.Memory[..length];
                    var action = MemoryMarshal.Read<int>(packet.Span[..4]);
                    length = MemoryMarshal.Read<int>(packet.Span.Slice(4, 4));
                    await callback(action, length == 0 ? ReadOnlyMemory<byte>.Empty : packet.Slice(8, length)).ConfigureAwait(false);
                } finally {
                    _buffer.Clear();
                }
            }
        }
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

    public async Task NativeReceiveAsync(Func<ReadOnlyMemory<byte>, ValueTask<int>> callback, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var memory = owner.Memory;
        while (!stoppingToken.IsCancellationRequested) {
            var result = await websocket.ReceiveAsync(memory, stoppingToken).ConfigureAwait(false);
            _buffer.Write(memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    using var plaintext = _aes.Decode(_buffer.WrittenSpan, out int length);
                    await callback(plaintext.Memory.Slice(0, length)).ConfigureAwait(false);
                } finally {
                    _buffer.Clear();
                }
            }
        }
    }

    public async Task NativeReceiveAsync(Func<ReadOnlyMemory<byte>, Task> callback, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        while (!stoppingToken.IsCancellationRequested) {
            var result = await websocket.ReceiveAsync(owner.Memory, stoppingToken).ConfigureAwait(false);
            _buffer.Write(owner.Memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    using var plaintext = _aes.Decode(_buffer.WrittenSpan, out int length);
                    await callback(plaintext.Memory.Slice(0, length)).ConfigureAwait(false);
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
}