#pragma warning disable IDE0079 // 请删除不必要的忽略
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

using System.Buffers;
using System.Net.Sockets;

namespace System.Net.WebSockets;

public static partial class Extensions
{
    public static Task CopyToAsync(this CustomWebSocket websocket, Socket socket, CancellationToken cancellationToken)
    {
        return websocket.NativeReceiveAsync((buffer) => socket.SendAsync(buffer, cancellationToken), cancellationToken);
    }

    public static async Task CopyToAsync(this Socket socket, CustomWebSocket websocket, CancellationToken cancellationToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var buffer = owner.Memory;
        while (!cancellationToken.IsCancellationRequested) {
            var length = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length == 0) {
                break;
            }
            await websocket.NativeSendAsync(buffer[..length], cancellationToken).ConfigureAwait(false);
        }
    }

    public static Task CopyToAsync(this WebSocketFactory websocket, Socket socket, CancellationToken cancellationToken)
    {
        return websocket.NativeReceiveAsync((buffer) => socket.SendAsync(buffer, cancellationToken), cancellationToken);
    }

    public static async Task CopyToAsync(this Socket socket, WebSocketFactory websocket, CancellationToken cancellationToken)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(131072);
        var buffer = owner.Memory;
        while (!cancellationToken.IsCancellationRequested) {
            var length = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length == 0) {
                break;
            }
            await websocket.NativeSendAsync(buffer[..length], cancellationToken).ConfigureAwait(false);
        }
    }
}