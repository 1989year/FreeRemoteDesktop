using System.Buffers;
using System.Net.Sockets;

namespace Custom.WebSocket.Relay;

public static partial class Extensions
{
    public static async Task CopyToAsync(this CustomWebSocketRelay source, Socket dest, CancellationToken cancellationToken)
    {
        try {
            while (!cancellationToken.IsCancellationRequested) {
                await source.ReceiveAsync(async (buffer) => {
                    await dest.SendAsync(buffer, cancellationToken).ConfigureAwait(false);
                }, cancellationToken).ConfigureAwait(false);
            }
        } catch (Exception) {
        }
    }

    public static async Task CopyToAsync(this Socket source, CustomWebSocketRelay dest, CancellationToken cancellationToken)
    {
        try {
            using var buffer = MemoryPool<byte>.Shared.Rent(131072);
            while (!cancellationToken.IsCancellationRequested) {
                var result = await source.ReceiveAsync(buffer.Memory, cancellationToken).ConfigureAwait(false);
                await dest.SendAsync(buffer.Memory.Slice(0, result), cancellationToken).ConfigureAwait(false);
            }
        } catch (Exception) {
        }
    }
}