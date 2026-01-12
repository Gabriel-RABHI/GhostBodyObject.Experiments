using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed class TransactionChecksum : IDisposable
    {
        private readonly XxHash3 _hasher;

        public TransactionChecksum()
        {
            // System.IO.Hashing.XxHash3 is optimized for speed and SIMD.
            _hasher = new XxHash3();
        }

        /// <summary>
        /// Writes a raw memory block to the running hash.
        /// Fast: Zero allocations, uses Spans.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte* data, int size)
        {
            // Create a span around the pointer. This is a stack-only struct operation (fast).
            var span = new ReadOnlySpan<byte>(data, size);
            _hasher.Append(span);
        }

        /// <summary>
        /// Writes any unmanaged struct (int, float, custom structs) to the running hash.
        /// Fast: No boxing, treats the struct memory directly as bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : unmanaged
        {
            // Treat the reference of 'value' as a Span of bytes.
            // This avoids copying the struct to a byte array.
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref value, 1)
            );

            _hasher.Append(bytes);
        }

        /// <summary>
        /// Writes a standard byte span (useful for managed arrays/buffers).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> data)
        {
            _hasher.Append(data);
        }

        /// <summary>
        /// Returns the current 64-bit checksum.
        /// </summary>
        public ulong GetHash()
        {
            // XxHash3 produces a 64-bit hash. 
            // We retrieve it into a ulong (little-endian by default in this API).
            Span<byte> destination = stackalloc byte[sizeof(ulong)];
            _hasher.GetCurrentHash(destination);
            return MemoryMarshal.Read<ulong>(destination);
        }

        /// <summary>
        /// Resets the hasher for a new transaction validation.
        /// </summary>
        public void Reset()
        {
            _hasher.Reset();
        }

        public void Dispose()
        {
            // XxHash3 usually doesn't hold unmanaged resources, but good practice if implementation changes.
            // Currently, System.IO.Hashing implementation is purely managed/stack based logic 
            // but keeping Dispose ensures forward compatibility.
        }
    }
}
