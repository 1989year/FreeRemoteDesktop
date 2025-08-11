using System.Buffers;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

public static class Extensions
{
    public static async Task CopyToAsync(this WebSocket source, WebSocket dest, CancellationToken cancellationToken)
    {
        using (var owner = MemoryPool<byte>.Shared.Rent(1024 * 1024 * 2)) {
            var buffer = owner.Memory;
            try {
                while (!cancellationToken.IsCancellationRequested) {
                    var result = await source.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    await dest.SendAsync(buffer.Slice(0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken).ConfigureAwait(false);
                }
            } catch (Exception) {
            }
        }
    }
}