using System.Buffers;
using System.Security.Cryptography;

namespace System.Net.WebSockets;

public sealed class CustomAesGcm(ReadOnlySpan<byte> key) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly AesGcm _aes = new(key, 16);
    private bool _disposed;

    public IMemoryOwner<byte> Encode(ReadOnlySpan<byte> plaintext, out int length)
    {
        length = 12 + plaintext.Length + 16;
        var buffer = MemoryPool<byte>.Shared.Rent(length);
        try {
            var memory = buffer.Memory[..length];
            RandomNumberGenerator.Fill(memory.Span[..12]);
            lock (_lock) {
                _aes.Encrypt(
                    memory.Span[..12],//nonce
                    plaintext,  // plaintext
                    memory.Span.Slice(12, plaintext.Length), // ciphertext
                    memory.Span.Slice(12 + plaintext.Length, 16) // tag
                    );
            }
            return buffer;
        } catch (Exception) {
            buffer.Dispose();
            throw;
        }
    }

    public IMemoryOwner<byte> Decode(ReadOnlySpan<byte> data, out int length)
    {
        length = data.Length - 12 - 16;
        var plaintext = MemoryPool<byte>.Shared.Rent(length);
        try {
            lock (_lock) {
                _aes.Decrypt(
                    data[..12], //nonce
                    data.Slice(12, length), // ciphertext
                    data.Slice(12 + length, 16),//tag
                    plaintext.Memory.Span[..length] // plaintext
                );
            }
            return plaintext;
        } catch (Exception) {
            plaintext.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, true)) {
            return;
        }
        _aes.Dispose();
        GC.SuppressFinalize(this);
    }
}