#define LOW_MEM_OVERHEAD

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace GhostBodyObject.Common.Memory
{

    /// <summary>
    /// Computes memory block sizes using a "Small Float" representation.
    /// Divides memory space into power-of-two ranges, slicing each range into 8 linear steps.
    /// This guarantees a worst-case fragmentation of 12.5%.
    /// </summary>
    public static class ChunkSizeComputation
    {
#if LOW_MEM_OVERHEAD
        // 12.5% Overhead (1/8th waste)
        // Slower resize growth, tighter memory packing.
        private const int MantissaBits = 3;
        public const int MaxIndex = 190;
#else
        // 25% Overhead (1/4th waste)
        // Faster resize growth, slightly looser memory packing.
        private const int MantissaBits = 2;
        public const int MaxIndex = 99;
#endif
        private const int MantissaMask = (1 << MantissaBits) - 1;
        private const int BucketSize = 1 << MantissaBits;
        private const int MinAlignShift = 6;
        private const int IndexBias = (MinAlignShift << MantissaBits) - BucketSize;
        public const uint MaximumBlockSize = 0x7FFFFFFF; // ~2GB (Safe int limit)

        /// <summary>
        /// Converts a byte size into a step index.
        /// Complexity: O(1) - roughly 5-10 CPU cycles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SizeToIndex(uint size)
        {
            if (size <= 64)
                return 0;
            uint s = size - 1;
            int lzc = BitOperations.LeadingZeroCount(s);
            int log2 = 32 - lzc;
            int shift = log2 - MantissaBits - 1;
            if (shift < MinAlignShift)
            {
                shift = MinAlignShift;
                return (ushort)(s >> shift);
            }
            int index = (int)(s >> shift);
            int exponentOffset = (shift - MinAlignShift) << MantissaBits;
            return (ushort)(index + exponentOffset + 1);
        }

        /// <summary>
        /// Converts a step index back to physical byte size.
        /// Complexity: O(1).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint IndexToSize(ushort index)
        {
            if (index < BucketSize)
                return (uint)(index + 1) << MinAlignShift;
            int exponent = index >> MantissaBits;
            int mantissa = index & MantissaMask;
            int shift = exponent + MinAlignShift + MantissaBits - 1;
            uint size = (uint)((BucketSize | mantissa) << (shift - MantissaBits));
            return size;
        }

        /// <summary>
        /// Computes the loss (overhead) bytes.
        /// </summary>
        public static ulong Loss(ushort index, uint size)
        {
            uint physical = IndexToSize(index);
            return physical > size ? physical - size : 0;
        }

        /// <summary>
        /// Returns true if the index can hold the requested size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Fit(ushort index, uint size)
        {
            return IndexToSize(index) >= size;
        }

        /// <summary>
        /// Compute the expanssion of a memory block.
        /// </summary>
        /// <param name="s">The physical size</param>
        /// <returns>A physical size larger than the given size</returns>
        public static uint Expand(uint s) => IndexToSize((ushort)(SizeToIndex(s) + 1));

        /// <summary>
        /// Compute the shrink of a memory block.
        /// </summary>
        /// <param name="s">The physical size for wich compute a shrink new size</param>
        /// <returns>The size, shrinked</returns>
        public static uint Shrink(uint s) => IndexToSize((ushort)(SizeToIndex(s) - 1));

        /// <summary>
        /// Determines whether the underlying storage should be reduced in size based on the specified new size and the
        /// current capacity index.
        /// </summary>
        /// <param name="currentIndex">The index representing the current capacity of the storage. Must correspond to a valid capacity value.</param>
        /// <param name="newSize">The proposed new size of the storage. If this value is less than half of the current capacity, shrinking is
        /// recommended.</param>
        /// <returns>true if the storage should be shrunk; otherwise, false.</returns>
        public static bool ShouldShrink(ushort currentIndex, uint newSize)
        {
            uint currentCapacity = IndexToSize(currentIndex);
            return newSize < (currentCapacity / 2);
        }
    }
}
