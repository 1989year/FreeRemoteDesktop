#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
using System.Buffers;
using System.Runtime.InteropServices;

namespace System.Net.WebSockets;

public class CustomClientWebSocket : IDisposable
{
    private readonly ArrayBufferWriter<byte> _buffer;
    public readonly ClientWebSocket _websocket;
    private bool _disposed;

    public CustomClientWebSocket()
    {
        _buffer = new ArrayBufferWriter<byte>();
        _websocket = new ClientWebSocket();
        _websocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    }

    public Task ConnectAsync(string uri, CancellationToken stoppingToken)
    {
        return _websocket.ConnectAsync(new Uri(uri), stoppingToken);
    }

    public async Task ReceiveAsync(Func<int, ReadOnlyMemory<byte>, Task> callback, CancellationToken stoppingToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var memory = owner.Memory;
        while (!stoppingToken.IsCancellationRequested) {
            var result = await _websocket.ReceiveAsync(memory, stoppingToken).ConfigureAwait(false);
            _buffer.Write(memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    var packet = _buffer.WrittenMemory;
                    var action = MemoryMarshal.Read<int>(packet.Span[..4]);
                    var length = MemoryMarshal.Read<int>(packet.Span.Slice(4, 4));
                    await callback(action, length == 0 ? ReadOnlyMemory<byte>.Empty : packet.Slice(8, length)).ConfigureAwait(false);
                } catch (Exception) {
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
        bytes.Span.CopyTo(buffer.Span[8..]);
        return _websocket.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true)) {
            return;
        }
        _buffer.Clear();
        _websocket.Dispose();
        GC.SuppressFinalize(this);
    }
}