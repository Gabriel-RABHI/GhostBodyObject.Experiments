#define PER_THREAD

using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Memory
{
    /// <summary>
    /// Provides thread-local memory allocation from a shared, pinned buffer for efficient interop and temporary data
    /// scenarios.
    /// </summary>
    /// <remarks>Allocations are performed from a buffer that is pinned in memory, which can improve
    /// performance when passing memory to unmanaged code. Each thread maintains its own buffer, and allocations are
    /// reused until the buffer's capacity is exceeded. This class is intended for scenarios where fast, temporary
    /// allocations are needed and the lifetime of the allocated memory is short. Allocated memory is not automatically
    /// cleared or released until the buffer is replaced.</remarks>
    public static class ManagedArenaAllocator
    {
        public const int DefaultPageSize = 64 * 1024; // 256KB

        [ThreadStatic]
        private static byte[]? _buffer;
        [ThreadStatic]
        private static int _offset;

        /// <summary>
        /// Allocates a block of memory of the specified size from a shared, pinned buffer. The memory is released when no longer referenced.
        /// </summary>
        /// <remarks>The returned memory is backed by a pinned buffer, which may improve performance for
        /// interop scenarios. Subsequent allocations may reuse the same buffer until its capacity is
        /// exceeded.</remarks>
        /// <param name="size">The number of bytes to allocate. Must be less than or equal to the default page size.</param>
        /// <returns>A Memory<byte> instance representing the allocated block of memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified size exceeds the default page size.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Memory<byte> Allocate(int size)
        {
            if ((uint)size > DefaultPageSize)
                throw new ArgumentOutOfRangeException(nameof(size));

            byte[]? buffer = _buffer;
            int offset = _offset;

            if (buffer == null || (uint)(offset + size) > (uint)buffer.Length)
            {
                buffer = GC.AllocateUninitializedArray<byte>(DefaultPageSize, pinned: true);
                _buffer = buffer;
                offset = 0;
            }
            _offset = offset + size;

            return new Memory<byte>(buffer, offset, size);
        }
    }
}
