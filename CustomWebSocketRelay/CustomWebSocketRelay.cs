using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Custom.WebSocket.Relay;

public class CustomWebSocketRelay : IDisposable
{
    private readonly ArrayBufferWriter<byte> _buffer = new();
    private bool _disposed;

    private readonly ClientWebSocket _websocket;

    public CustomWebSocketRelay(bool enableDeflate = false)
    {
        _websocket = new ClientWebSocket() {
            Options = {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
                KeepAliveTimeout = TimeSpan.FromSeconds(60)
            }
        };
        _websocket.Options.DangerousDeflateOptions = new WebSocketDeflateOptions();
        _websocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
    }

    public async Task<string> ConnectAsync(Uri uri, bool owner, CancellationToken cancellationToken)
    {
        var builder = new UriBuilder(uri) {
            Path = $"/v1/bridge"
        };
        await _websocket.ConnectAsync(builder.Uri, cancellationToken).ConfigureAwait(false);
        if (owner) {
            using var buffer = MemoryPool<byte>.Shared.Rent(64);
            var result = await _websocket.ReceiveAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
            ArgumentOutOfRangeException.ThrowIfNotEqual(result.Count, 16);
            var token = MemoryMarshal.Read<Guid>(buffer.Memory.Span.Slice(0, 16));
            builder.Query = $"?token={token}";
            return builder.ToString();
        }
        return string.Empty;
    }

    public async Task<string> ConnectAsync(string url, bool owner, CancellationToken cancellationToken)
    {
        return await ConnectAsync(new Uri(url), owner, cancellationToken).ConfigureAwait(false);
    }

    public async Task WaitAsync(int millisecondsDelay, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(millisecondsDelay);
        await _websocket.ReceiveAsync(Memory<byte>.Empty, cts.Token).ConfigureAwait(false);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await _websocket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(string s, CancellationToken cancellationToken)
    {
        await SendAsync(Encoding.UTF8.GetBytes(s), cancellationToken).ConfigureAwait(false);
    }

    public async Task SwapAsync(IPAddress addr, int port, CancellationToken cancellationToken)
    {
        using var local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) {
            NoDelay = true
        };
        await local.ConnectAsync(addr, port, cancellationToken).ConfigureAwait(false);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try {
            await Task.WhenAny(local.CopyToAsync(this, cts.Token),
                this.CopyToAsync(local, cts.Token)).ConfigureAwait(false);
        } catch (Exception) {
            cts.Cancel();
        }
    }

    public async Task SwapAsync(Socket local, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try {
            await Task.WhenAny(local.CopyToAsync(this, cts.Token),
                this.CopyToAsync(local, cts.Token)).ConfigureAwait(false);
        } catch (Exception) {
            cts.Cancel();
        }
    }

    public async Task ReceiveAsync(Func<ReadOnlyMemory<byte>, Task> func, CancellationToken cancellationToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        while (!cancellationToken.IsCancellationRequested) {
            var result = await _websocket.ReceiveAsync(owner.Memory, cancellationToken).ConfigureAwait(false);
            _buffer.Write(owner.Memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    await func(_buffer.WrittenMemory).ConfigureAwait(false);
                } finally {
                    _buffer.Clear();
                }
            }
        }
    }

    public async Task ReceiveAsync(Func<int, ReadOnlyMemory<byte>, Task> func, CancellationToken cancellationToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        while (!cancellationToken.IsCancellationRequested) {
            var result = await _websocket.ReceiveAsync(owner.Memory, cancellationToken).ConfigureAwait(false);
            _buffer.Write(owner.Memory.Span[..result.Count]);
            if (result.EndOfMessage) {
                try {
                    int action = MemoryMarshal.Read<int>(_buffer.WrittenSpan);
                    int length = MemoryMarshal.Read<int>(_buffer.WrittenSpan.Slice(4));
                    await func(action, length <= 0 ? ReadOnlyMemory<byte>.Empty : _buffer.WrittenMemory.Slice(8, length)).ConfigureAwait(false);
                } finally {
                    _buffer.Clear();
                }
            }
        }
    }

    public async Task SendAsync(int action, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        var length = 8 + bytes.Length;
        using var memory = MemoryPool<byte>.Shared.Rent(length);
        var buffer = memory.Memory;
        MemoryMarshal.Write(buffer.Span, action);
        MemoryMarshal.Write(buffer.Span.Slice(4), bytes.Length);
        if (bytes.Length > 0) {
            bytes.CopyTo(buffer.Slice(8));
        }
        await _websocket.SendAsync(buffer.Slice(0, length), WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendAsync(int action, string s, CancellationToken cancellationToken)
    {
        await SendAsync(action, Encoding.UTF8.GetBytes(s), cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true)) {
            return;
        }
        try {
            _websocket?.Dispose();
        } catch (Exception) {
        }
        _buffer.Clear();
        GC.SuppressFinalize(this);
    }
}