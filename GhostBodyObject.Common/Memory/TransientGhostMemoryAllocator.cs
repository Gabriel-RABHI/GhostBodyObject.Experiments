#define PER_THREAD

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Common.Memory
{
    /// <summary>
    /// Provides static methods for allocating and resizing memory blocks with over-allocation to facilitate efficient
    /// resizing operations. The memory is released when no longer referenced.
    /// </summary>
    /// <remarks>The GhostMemoryAllocator is designed to optimize memory management for scenarios where
    /// frequent resizing of byte buffers is required. It over-allocates memory blocks to reduce the need for
    /// reallocation during growth, and attempts to reuse existing allocations when possible. This class is thread-safe
    /// and intended for advanced use cases where manual control over memory allocation is beneficial. All methods
    /// operate on managed memory and do not interact with unmanaged resources.</remarks>
    public static class TransientGhostMemoryAllocator
    {
        private const int LargeBlockThreshold = 32 * 1024; // 32KB

        /// <summary>
        /// Allocates a memory block with over-allocation to ease future resizing.
        /// </summary>
        /// <param name="size">The logical size required by the user.</param>
        /// <returns>A Memory&lt;byte&gt; slice of the requested size.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe PinnedMemory<byte> Allocate(int size)
        {
            // Use unsigned comparison: negative values become large positive, failing the check
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));

            if ((uint)size == 0)
                return PinnedMemory<byte>.Empty;

            // Single lookup: size -> physical size (combines SizeToIndex + IndexToSize)
            uint physicalSize = ChunkSizeComputation.SizeToPhysicalSize((uint)size);

            if (physicalSize > LargeBlockThreshold)
            {
                var array = GC.AllocateUninitializedArray<byte>((int)physicalSize, pinned: true);
                return new PinnedMemory<byte>(array, 0, size);
            }
            else
            {
                // Allocate physical size from arena, but return logical size directly
                // Avoids the Slice call overhead by constructing with correct length
                var memory = ManagedArenaAllocator.Allocate((int)physicalSize);
                return new PinnedMemory<byte>(memory.MemoryOwner, memory.Ptr, size);
            }
        }

        /// <summary>
        /// Resizes a memory block. If the new size fits within the previously over-allocated
        /// physical capacity, it avoids reallocation.
        /// </summary>
        /// <param name="block">The current memory block reference (will be updated).</param>
        /// <param name="newSize">The new required size.</param>
        /// <returns>True if the memory block base address changed (reallocation occurred), false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool Resize(ref PinnedMemory<byte> block, int newSize)
        {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));
            if (block.IsEmpty)
            {
                block = Allocate(newSize);
                return true; // Address changed (was empty, now allocated)
            }
            if (newSize == 0)
            {
                block = PinnedMemory<byte>.Empty;
                return true; // Address changed (was allocated, now empty)
            }

            // -------- Get Underlying Array info
            // We declare segment here so it stays in scope for the rest of the method
            var segment = block.MemoryOwner as byte[];

            // -------- Determine TRUE Physical Capacity
            if (segment != null)
            {
                // Identify if this is a Dedicated Large Block or a Managed Arena Block.
                // Dedicated blocks are allocated via GC.AllocateUninitializedArray with the specific chunk size.
                // Arena blocks are slices of a fixed DefaultPageSize (128KB) array.
                //
                // Heuristic:
                // 1. If segment length != 128KB, it MUST be Dedicated (since Arena pages are always 128KB).
                // 2. If segment length == 128KB, it could be ambiguous.
                //    - If block.Length > 32KB (LargeBlockThreshold), it MUST be Dedicated (Arena only handles <= 32KB).
                //    - If block.Length <= 32KB, we treat it as Arena. 
                //      (Even if it was a Dedicated 128KB block shrunk to < 32KB, treating it as Arena is safe 
                //       because we will calculate a smaller capacity and potentially reallocate, which is valid).
                
                bool isDedicated = (segment.Length != ManagedArenaAllocator.DefaultPageSize) || (block.Length > LargeBlockThreshold);

                if (isDedicated)
                {
                    // -------- Dedicated Large Block
                    int capacity = segment.Length;

                    if (newSize <= capacity)
                    {
                        // Check for major shrink (< 50% of capacity)
                        // This prevents holding onto large arrays for small data (Salami Slicing Drift)
                        if (newSize < capacity / 2)
                        {
                            goto Reallocate;
                        }

                        // Reuse existing array
                        // Dedicated blocks always start at offset 0
                        block = new PinnedMemory<byte>(segment, 0, newSize);
                        return false; // Same base address
                    }
                }
                else
                {
                    // -------- Managed Arena Block
                    // Capacity is determined by the chunk size of the current length.
                    // Single lookup: combines SizeToIndex + IndexToSize
                    uint capacity = ChunkSizeComputation.SizeToPhysicalSize((uint)block.Length);

                    if (newSize <= capacity)
                    {
                        // Check for drastic shrink (< 50% of bucket capacity)
                        if (newSize < capacity / 2)
                        {
                            goto Reallocate;
                        }

                        // Reuse existing block (slice)
                        // We must preserve the pointer (offset)
                        block = new PinnedMemory<byte>(block.MemoryOwner, block.Ptr, newSize);
                        return false; // Same base address
                    }
                }
            }

            Reallocate:
            // -------- Fallback: Reallocate and Copy
            var newBlock = Allocate(newSize);
            var bytesToCopy = Math.Min(block.Length, newSize);
            block.Slice(0, bytesToCopy).CopyTo(newBlock);
            block = newBlock;
            return true; // Address changed
        }
    }
}
