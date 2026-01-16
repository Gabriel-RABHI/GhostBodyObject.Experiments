/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

#define LOW_MEM_OVERHEAD

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Memory
{

    /// <summary>
    /// Computes memory block sizes using a "Small Float" representation.
    /// Divides memory space into power-of-two ranges, slicing each range into 8 linear steps.
    /// This guarantees a worst-case fragmentation of 12.5%.
    /// Uses direct lookup tables for sizes up to 32KB for maximum performance.
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

        // Lookup table for IndexToSize - covers all indices
        private static readonly uint[] IndexToSizeTable;

        // Direct lookup table for SizeToIndex - covers sizes 0 to LookupTableSize-1
        // Each entry at position [size >> 6] gives the index for that 64-byte aligned size
        private const int SizeToIndexLookupLimit = 32 * 1024; // 32KB
        private const int SizeToIndexTableEntries = SizeToIndexLookupLimit >> MinAlignShift; // 512 entries
        private static readonly byte[] SizeToIndexTable; // byte is enough since indices are small for this range

        // Direct lookup table for SizeToPhysicalSize - maps size directly to physical size
        // Eliminates the need for SizeToIndex + IndexToSize for the common case
        private static readonly uint[] SizeToPhysicalSizeTable;

        static ChunkSizeComputation()
        {
            // Build IndexToSize lookup table
            IndexToSizeTable = new uint[MaxIndex + 1];
            for (int i = 0; i <= MaxIndex; i++)
            {
                IndexToSizeTable[i] = ComputeIndexToSize((ushort)i);
            }

            // Build SizeToIndex lookup table for sizes 1 to 32KB
            // Table is indexed by (size - 1) >> 6, giving the index for sizes in each 64-byte block
            SizeToIndexTable = new byte[SizeToIndexTableEntries];
            for (int i = 0; i < SizeToIndexTableEntries; i++)
            {
                // Size represented by this table entry: sizes from (i << 6) + 1 to ((i + 1) << 6)
                // We need the index that fits the maximum size in this block: (i + 1) << 6
                uint maxSizeInBlock = (uint)((i + 1) << MinAlignShift);
                SizeToIndexTable[i] = (byte)ComputeSizeToIndex(maxSizeInBlock);
            }

            // Build SizeToPhysicalSize lookup table - direct size to physical size mapping
            // Same indexing as SizeToIndexTable: indexed by (size - 1) >> 6
            SizeToPhysicalSizeTable = new uint[SizeToIndexTableEntries];
            for (int i = 0; i < SizeToIndexTableEntries; i++)
            {
                byte index = SizeToIndexTable[i];
                SizeToPhysicalSizeTable[i] = IndexToSizeTable[index];
            }
        }

        /// <summary>
        /// Internal computation for building the IndexToSize lookup table.
        /// </summary>
        private static uint ComputeIndexToSize(ushort index)
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
        /// Internal computation for building the SizeToIndex lookup table and fallback.
        /// </summary>
        private static ushort ComputeSizeToIndex(uint size)
        {
            if (size <= 64)
                return 0;

            uint s = size - 1;
            int log2 = 31 - BitOperations.LeadingZeroCount(s);
            int shift = log2 - MantissaBits;

            int clampedShift = shift < MinAlignShift ? MinAlignShift : shift;
            int index = (int)(s >> clampedShift);

            int exponentOffset = (clampedShift - MinAlignShift) << MantissaBits;
            int adjustment = shift < MinAlignShift ? 0 : 1;

            return (ushort)(index + exponentOffset + adjustment);
        }

        /// <summary>
        /// Converts a byte size into a step index.
        /// Complexity: O(1) - direct lookup for sizes ≤ 32KB, computed for larger.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort SizeToIndex(uint size)
        {
            // Fast path: direct table lookup for small sizes (covers ~99% of allocations)
            if (size <= SizeToIndexLookupLimit)
            {
                // Handle size == 0 edge case
                if (size == 0)
                    return 0;
                // Index by (size - 1) >> 6 to get the right bucket
                return SizeToIndexTable[(size - 1) >> MinAlignShift];
            }

            // Slow path: compute for large sizes
            return ComputeSizeToIndex(size);
        }

        /// <summary>
        /// Converts a byte size directly to its physical (over-allocated) size.
        /// Combines SizeToIndex and IndexToSize into a single lookup for sizes ≤ 32KB.
        /// Complexity: O(1) - single array lookup for small sizes.
        /// </summary>
        /// <param name="size">The logical size requested.</param>
        /// <returns>The physical size that should be allocated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint SizeToPhysicalSize(uint size)
        {
            // Fast path: single lookup for small sizes
            if (size <= SizeToIndexLookupLimit)
            {
                if (size == 0)
                    return 0;
                return SizeToPhysicalSizeTable[(size - 1) >> MinAlignShift];
            }

            // Slow path: compute for large sizes
            return IndexToSizeTable[ComputeSizeToIndex(size)];
        }

        /// <summary>
        /// Converts a step index back to physical byte size.
        /// Complexity: O(1) - single array lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint IndexToSize(ushort index)
        {
            return IndexToSizeTable[index];
        }

        /// <summary>
        /// Computes the loss (overhead) bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Loss(ushort index, uint size)
        {
            uint physical = IndexToSizeTable[index];
            return physical - size;
        }

        /// <summary>
        /// Returns true if the index can hold the requested size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Fit(ushort index, uint size)
        {
            return IndexToSizeTable[index] >= size;
        }

        /// <summary>
        /// Compute the expansion of a memory block.
        /// </summary>
        /// <param name="s">The physical size</param>
        /// <returns>A physical size larger than the given size</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Expand(uint s) => IndexToSizeTable[SizeToIndex(s) + 1];

        /// <summary>
        /// Compute the shrink of a memory block.
        /// </summary>
        /// <param name="s">The physical size for which compute a shrink new size</param>
        /// <returns>The size, shrinked</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Shrink(uint s) => IndexToSizeTable[SizeToIndex(s) - 1];

        /// <summary>
        /// Determines whether the underlying storage should be reduced in size based on the specified new size and the
        /// current capacity index.
        /// </summary>
        /// <param name="currentIndex">The index representing the current capacity of the storage. Must correspond to a valid capacity value.</param>
        /// <param name="newSize">The proposed new size of the storage. If this value is less than half of the current capacity, shrinking is
        /// recommended.</param>
        /// <returns>true if the storage should be shrunk; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldShrink(ushort currentIndex, uint newSize)
        {
            return newSize < (IndexToSizeTable[currentIndex] >> 1);
        }
    }
}
