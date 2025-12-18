#define PER_THREAD

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
        public static Memory<byte> Allocate(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (size == 0)
                return Memory<byte>.Empty;

            ushort index = ChunkSizeComputation.SizeToIndex((uint)size);
            uint physicalSize = ChunkSizeComputation.IndexToSize(index);

            if (physicalSize > LargeBlockThreshold)
            {
                var array = GC.AllocateUninitializedArray<byte>((int)physicalSize);
                return new Memory<byte>(array, 0, size);
            }
            else
            {
                var memory = ManagedArenaAllocator.Allocate((int)physicalSize);
                return memory.Slice(0, size);
            }
        }

        /// <summary>
        /// Resizes a memory block. If the new size fits within the previously over-allocated
        /// physical capacity, it avoids reallocation.
        /// </summary>
        /// <param name="block">The current memory block reference (will be updated).</param>
        /// <param name="newSize">The new required size.</param>
        public static void Resize(ref Memory<byte> block, int newSize)
        {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize));
            if (block.IsEmpty)
            {
                block = Allocate(newSize);
                return;
            }
            if (newSize == 0)
            {
                block = Memory<byte>.Empty;
                return;
            }

            // -------- Get Underlying Array info
            // We declare segment here so it stays in scope for the rest of the method
            ArraySegment<byte> segment = default;
            bool hasUnderlyingArray = MemoryMarshal.TryGetArray(block, out segment) && segment.Array != null;

            // -------- Determine TRUE Physical Capacity
            uint currentPhysicalCapacity;
            bool isLargeDedicatedArray = false;

            if (hasUnderlyingArray)
            {
                uint actualArrayLength = (uint)segment.Array!.Length;

                // Check if this is a large dedicated block or an Arena page
                if (actualArrayLength > ManagedArenaAllocator.DefaultPageSize)
                {
                    currentPhysicalCapacity = actualArrayLength;
                    isLargeDedicatedArray = true;
                }
                else
                {
                    // It is a slice inside an Arena Page. 
                    // We use the inferred capacity logic.
                    ushort assumedIndex = ChunkSizeComputation.SizeToIndex((uint)block.Length);
                    currentPhysicalCapacity = ChunkSizeComputation.IndexToSize(assumedIndex);
                }
            }
            else
            {
                // Fallback for unknown memory sources
                ushort assumedIndex = ChunkSizeComputation.SizeToIndex((uint)block.Length);
                currentPhysicalCapacity = ChunkSizeComputation.IndexToSize(assumedIndex);
            }

            // -------- Decide to Reallocate
            bool fitsInCapacity = newSize <= currentPhysicalCapacity;
            ushort capacityIndex = ChunkSizeComputation.SizeToIndex(currentPhysicalCapacity);
            bool shouldShrink = fitsInCapacity && ChunkSizeComputation.ShouldShrink(capacityIndex, (uint)newSize);

            if (fitsInCapacity && !shouldShrink && hasUnderlyingArray)
            {
                // Optimization: Expand or Shrink In-Place
                // We use the segment.Array and segment.Offset to create a NEW view 
                // that spans the new size.

                // Safety Check: Ensure we don't go out of the physical array bounds
                if (segment.Offset + newSize <= segment.Array!.Length)
                {
                    block = new Memory<byte>(segment.Array, segment.Offset, newSize);
                    return;
                }
            }

            // -------- Fallback: Reallocate and Copy
            var newBlock = Allocate(newSize);
            var bytesToCopy = Math.Min(block.Length, newSize);
            block.Slice(0, bytesToCopy).CopyTo(newBlock);
            block = newBlock;
        }
    }
}
