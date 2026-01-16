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

using GhostBodyObject.Common.Memory;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Contracts
{

    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 24)]
    public unsafe abstract class BodyBase
    {
        // -----------------------------------------------------------------
        // Small Array Limits Constants
        // -----------------------------------------------------------------

        /// <summary>
        /// Maximum array offset for small arrays (ushort max = 65535).
        /// </summary>
        public const int SmallArrayMaxOffset = ushort.MaxValue;
        
        /// <summary>
        /// Maximum array element count for small arrays (11 bits = 2047).
        /// </summary>
        public const int SmallArrayMaxLength = 2047;

        [FieldOffset(0)]
        protected internal object _owner;

        [FieldOffset(8)]
        protected internal IntPtr _vTablePtr;

        [FieldOffset(16)]
        protected internal PinnedMemory<byte> _data;

        // -----------------------------------------------------------------
        // VTable Header Access
        // -----------------------------------------------------------------

        internal VectorTableHeader* _vTableHeader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (VectorTableHeader*)_vTablePtr;
        }

        protected unsafe int TotalSize
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                var _vt = (VectorTableHeader*)_vTablePtr;
                if (_vt->LargeArrays)
                {
                    ArrayMapLargeEntry* last = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset + ((_vt->ArrayMapLength - 1) * sizeof(ArrayMapLargeEntry)));
                    return last->ArrayEndOffset;
                }
                else
                {
                    ArrayMapSmallEntry* last = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset + ((_vt->ArrayMapLength - 1) * sizeof(ArrayMapSmallEntry)));
                    return last->ArrayEndOffset;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int GetTotalSize(BodyBase body)
        {
            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
            {
                ArrayMapLargeEntry* last = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset + ((_vt->ArrayMapLength - 1) * sizeof(ArrayMapLargeEntry)));
                return last->ArrayEndOffset;
            }
            else
            {
                ArrayMapSmallEntry* last = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset + ((_vt->ArrayMapLength - 1) * sizeof(ArrayMapSmallEntry)));
                return last->ArrayEndOffset;
            }
        }

        public GhostHeader* Header
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (GhostHeader*)_data.Ptr;
            }
        }

        public GhostId Id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Header->Id;
        }

        public GhostStatus Status
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Header->Status;
        }

        public bool Inserted => Status == GhostStatus.Inserted;

        public bool Mapped => Status == GhostStatus.Mapped;

        public bool MappedModified => Status == GhostStatus.MappedModified;

        public bool MappedDeleted => Status == GhostStatus.MappedDeleted;

        public long TxnId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Header->TxnId;
        }

        // -----------------------------------------------------------------
        // Generic Array Swap Helpers
        // -----------------------------------------------------------------
        /// <summary>
        /// Replaces the contents of the array at the specified index with the data from the provided memory buffer.
        /// </summary>
        /// <remarks>This method updates the data of an existing array in place. If the size of the new
        /// data differs from the current array size, the underlying storage may be resized and other arrays may be
        /// shifted to accommodate the change. This shift respect the needed alignement. Arrays are sorted by value size.
        /// Callers should ensure that the source buffer is valid and that the index
        /// refers to an existing array in the Array Map.</remarks>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="src">A span containing the new data to copy into the target array. The length of this buffer must match
        /// the expected size for the array at the specified index.</param>
        /// <param name="arrayIndex">The zero-based index of the array to be updated.</param>
        public static unsafe void SwapAnyArray(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex)
        {
            // This method must move arrays taking care of alignements and offsets.
            // Arrays are sorted from largest to smallest value size: [8, 4, 4, 2, 2, 1, 1]
            // Key insight: Once aligned to the next array's value size, all subsequent arrays
            // are naturally aligned (since value sizes are decreasing).
            // This allows a single block copy for all subsequent arrays.
            if (arrayIndex < 0)
                return;
            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
            {
                SwapAnyArrayLarge(body, src, arrayIndex, _vt);
            }
            else
            {
                SwapAnyArraySmall(body, src, arrayIndex, _vt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AlignTo(int offset, int alignment)
        {
            if (alignment <= 1) return offset;
            int mask = alignment - 1;
            return (offset + mask) & ~mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SwapAnyArrayLarge(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            // Check that src length is a multiple of value size
            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;
            int arrayCount = _vt->ArrayMapLength;

            if (currentPhysicalSize == src.Length)
            {
                // -------- Exact fit: just copy data --------
                if (src.Length == 0)
                    return;
                fixed (byte* srcPtr = src)
                {
                    Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
                return;
            }

            // -------- Size changed: need to shift subsequent arrays --------
            int oldTotalSize = GetTotalSize(body);
            int newArrayEnd = currentOffset + src.Length;
            bool hasSubsequentArrays = arrayIndex + 1 < arrayCount;

            if (!hasSubsequentArrays)
            {
                // Last array: just resize and copy
                if (src.Length > currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref body._data, newArrayEnd))
                {
                    mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }

                if (src.Length < currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref body._data, newArrayEnd))
                {
                    mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                return;
            }

            // Get next array info for alignment
            ArrayMapLargeEntry* nextEntry = mapBase + arrayIndex + 1;
            int nextValueSize = nextEntry->ValueSize;
            int oldNextOffset = (int)nextEntry->ArrayOffset;
            int tailLength = oldTotalSize - oldNextOffset;

            // Align new position for subsequent arrays (all naturally aligned after this)
            // delta == 0 is possible when sizes differ but alignment padding absorbs the difference
            int newNextOffset = AlignTo(newArrayEnd, nextValueSize);
            int delta = newNextOffset - oldNextOffset;

            if (delta == 0)
            {
                // No shift needed, just copy new data
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                    mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                    return;
                }

                int newTotalSize = oldTotalSize + delta;

                if (delta > 0)
                {
                    // -------- Growing: resize first, then move tail backward (from end) --------
                    if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                    {
                        mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                        mapEntry = mapBase + arrayIndex;
                    }
                    // Move all subsequent arrays in one block (use memmove semantics for overlapping)
                    if (tailLength > 0)
                    {
                        Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                    }
                }
                else
                {
                    // -------- Shrinking: move tail forward first, then resize --------
                    if (tailLength > 0)
                        Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                    if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                    {
                        mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                        mapEntry = mapBase + arrayIndex;
                    }
                }

                // Update all subsequent array offsets with uniform delta
                for (int i = arrayIndex + 1; i < arrayCount; i++)
                {
                    ArrayMapLargeEntry* entry = mapBase + i;
                    entry->ArrayOffset = (uint)((int)entry->ArrayOffset + delta);
                }

                // Copy new data
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }

                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
            }
            else
            {
                // delta != 0: need to shift subsequent arrays
                int newTotalSize = oldTotalSize + delta;

                if (delta > 0)
                {
                    // -------- Growing: resize first, then move tail backward (from end) --------
                    if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                    {
                        mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                        mapEntry = mapBase + arrayIndex;
                    }
                    // Move all subsequent arrays in one block (use memmove semantics for overlapping)
                    if (tailLength > 0)
                    {
                        Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                    }
                }
                else
                {
                    // -------- Shrinking: move tail forward first, then resize --------
                    if (tailLength > 0)
                        Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                    if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                    {
                        mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                        mapEntry = mapBase + arrayIndex;
                    }
                }

                // Update all subsequent array offsets with uniform delta
                for (int i = arrayIndex + 1; i < arrayCount; i++)
                {
                    ArrayMapLargeEntry* entry = mapBase + i;
                    entry->ArrayOffset = (uint)((int)entry->ArrayOffset + delta);
                }

                // Copy new data
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }

                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SwapAnyArraySmall(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            // Check that src length is a multiple of value size
            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            // Check array length limit (11 bits = max 2047 elements)
            int newArrayLength = src.Length / valueSize;
            if (newArrayLength > SmallArrayMaxLength)
                throw new OverflowException($"Array length ({newArrayLength}) exceeds the maximum allowed for small arrays ({SmallArrayMaxLength} elements).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;
            int arrayCount = _vt->ArrayMapLength;

            if (currentPhysicalSize == src.Length)
            {
                // -------- Exact fit: just copy data --------
                if (src.Length == 0)
                    return;
                fixed (byte* srcPtr = src)
                {
                    Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
                return;
            }

            // -------- Size changed: need to shift subsequent arrays --------
            int oldTotalSize = GetTotalSize(body);
            int newArrayEnd = currentOffset + src.Length;
            
            // Check offset limit (ushort max = 65535)
            if (newArrayEnd > SmallArrayMaxOffset)
                throw new OverflowException($"Array end offset ({newArrayEnd}) exceeds the maximum allowed for small arrays ({SmallArrayMaxOffset} bytes).");

            bool hasSubsequentArrays = arrayIndex + 1 < arrayCount;

            if (!hasSubsequentArrays)
            {
                // Last array: just resize and copy
                if (src.Length > currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref body._data, newArrayEnd))
                {
                    mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }

                if (src.Length < currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref body._data, newArrayEnd))
                {
                    mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                return;
            }

            // Get next array info for alignment
            ArrayMapSmallEntry* nextEntry = mapBase + arrayIndex + 1;
            int nextValueSize = (int)nextEntry->ValueSize;
            int oldNextOffset = nextEntry->ArrayOffset;
            int tailLength = oldTotalSize - oldNextOffset;

            // Align new position for subsequent arrays (all naturally aligned after this)
            // delta == 0 is possible when sizes differ but alignment padding absorbs the difference
            int newNextOffset = AlignTo(newArrayEnd, nextValueSize);
            int delta = newNextOffset - oldNextOffset;

            // Check that new total size doesn't exceed small array limits
            int newTotalSize = oldTotalSize + delta;
            if (newTotalSize > SmallArrayMaxOffset)
                throw new OverflowException($"Total ghost size ({newTotalSize}) would exceed the maximum allowed for small arrays ({SmallArrayMaxOffset} bytes).");

            if (delta == 0)
            {
                // No shift needed, just copy new data
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }
                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                return;
            }

            if (delta > 0)
            {
                // -------- Growing: resize first, then move tail backward (from end) --------
                if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                {
                    mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                // Move all subsequent arrays in one block (use memmove semantics for overlapping)
                if (tailLength > 0)
                {
                    Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                }
            }
            else
            {
                // -------- Shrinking: move tail forward first, then resize --------
                if (tailLength > 0)
                {
                    Buffer.MemoryCopy(body._data.Ptr + oldNextOffset, body._data.Ptr + newNextOffset, tailLength, tailLength);
                }
                if (TransientGhostMemoryAllocator.Resize(ref body._data, newTotalSize))
                {
                    mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }
            }

            // Update all subsequent array offsets with uniform delta
            for (int i = arrayIndex + 1; i < arrayCount; i++)
            {
                ArrayMapSmallEntry* entry = mapBase + i;
                int newOffset = entry->ArrayOffset + delta;
                
                // Verify the new offset fits in ushort
                if (newOffset > SmallArrayMaxOffset || newOffset < 0)
                    throw new OverflowException($"Array offset ({newOffset}) exceeds the maximum allowed for small arrays ({SmallArrayMaxOffset} bytes).");
                    
                entry->ArrayOffset = (ushort)newOffset;
            }

            // Copy new data
            if (src.Length > 0)
            {
                fixed (byte* srcPtr = src)
                {
                    Unsafe.CopyBlockUnaligned(body._data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
            }
            mapEntry->ArrayLength = (uint)(src.Length / valueSize);
        }

        // -----------------------------------------------------------------
        // High-Performance Array Modification Helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Appends data to the end of the array at the specified index.
        /// </summary>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="src">The data to append.</param>
        /// <param name="arrayIndex">The zero-based index of the array.</param>
        public static unsafe void AppendToArray(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex)
        {
            if (arrayIndex < 0 || src.Length == 0)
                return;

            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
                AppendToArrayLarge(body, src, arrayIndex, _vt);
            else
                AppendToArraySmall(body, src, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AppendToArrayLarge(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;
            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: existing + new
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (currentPhysicalSize > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, currentPhysicalSize).CopyTo(combined);
            }
            src.CopyTo(combined.Slice(currentPhysicalSize));

            // Use SwapAnyArray to handle all the resizing/shifting logic
            SwapAnyArrayLarge(body, combined, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void AppendToArraySmall(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;
            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: existing + new
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (currentPhysicalSize > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, currentPhysicalSize).CopyTo(combined);
            }
            src.CopyTo(combined.Slice(currentPhysicalSize));

            // Use SwapAnyArray to handle all the resizing/shifting logic
            SwapAnyArraySmall(body, combined, arrayIndex, _vt);
        }

        /// <summary>
        /// Prepends data to the beginning of the array at the specified index.
        /// </summary>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="src">The data to prepend.</param>
        /// <param name="arrayIndex">The zero-based index of the array.</param>
        public static unsafe void PrependToArray(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex)
        {
            if (arrayIndex < 0 || src.Length == 0)
                return;

            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
                PrependToArrayLarge(body, src, arrayIndex, _vt);
            else
                PrependToArraySmall(body, src, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void PrependToArrayLarge(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;
            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: new + existing
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            src.CopyTo(combined);
            if (currentPhysicalSize > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, currentPhysicalSize).CopyTo(combined.Slice(src.Length));
            }

            // Use SwapAnyArray to handle all the resizing/shifting logic
            SwapAnyArrayLarge(body, combined, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void PrependToArraySmall(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;
            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: new + existing
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            src.CopyTo(combined);
            if (currentPhysicalSize > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, currentPhysicalSize).CopyTo(combined.Slice(src.Length));
            }

            // Use SwapAnyArray to handle all the resizing/shifting logic
            SwapAnyArraySmall(body, combined, arrayIndex, _vt);
        }

        /// <summary>
        /// Inserts data at the specified byte offset within the array at the specified index.
        /// </summary>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="src">The data to insert.</param>
        /// <param name="arrayIndex">The zero-based index of the array.</param>
        /// <param name="byteOffset">The byte offset within the array where data should be inserted.</param>
        public static unsafe void InsertIntoArray(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, int byteOffset)
        {
            if (arrayIndex < 0 || src.Length == 0)
                return;

            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
                InsertIntoArrayLarge(body, src, arrayIndex, byteOffset, _vt);
            else
                InsertIntoArraySmall(body, src, arrayIndex, byteOffset, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InsertIntoArrayLarge(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, int byteOffset, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;

            if (byteOffset > currentPhysicalSize)
                byteOffset = currentPhysicalSize;

            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: before + new + after
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
            }
            src.CopyTo(combined.Slice(byteOffset));
            if (byteOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + byteOffset, currentPhysicalSize - byteOffset)
                    .CopyTo(combined.Slice(byteOffset + src.Length));
            }

            SwapAnyArrayLarge(body, combined, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InsertIntoArraySmall(BodyBase body, ReadOnlySpan<byte> src, int arrayIndex, int byteOffset, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;

            if (byteOffset > currentPhysicalSize)
                byteOffset = currentPhysicalSize;

            int newPhysicalSize = currentPhysicalSize + src.Length;

            // Build combined data: before + new + after
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
            }
            src.CopyTo(combined.Slice(byteOffset));
            if (byteOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + byteOffset, currentPhysicalSize - byteOffset)
                    .CopyTo(combined.Slice(byteOffset + src.Length));
            }

            SwapAnyArraySmall(body, combined, arrayIndex, _vt);
        }

        /// <summary>
        /// Removes data from the array at the specified index.
        /// </summary>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="arrayIndex">The zero-based index of the array.</param>
        /// <param name="byteOffset">The byte offset where removal starts.</param>
        /// <param name="byteLength">The number of bytes to remove.</param>
        public static unsafe void RemoveFromArray(BodyBase body, int arrayIndex, int byteOffset, int byteLength)
        {
            if (arrayIndex < 0 || byteLength <= 0)
                return;

            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
                RemoveFromArrayLarge(body, arrayIndex, byteOffset, byteLength, _vt);
            else
                RemoveFromArraySmall(body, arrayIndex, byteOffset, byteLength, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void RemoveFromArrayLarge(BodyBase body, int arrayIndex, int byteOffset, int byteLength, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            if (byteLength % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and remove length ({byteLength}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;

            if (byteOffset >= currentPhysicalSize)
                return;

            // Clamp byteLength if it would go past the end
            if (byteOffset + byteLength > currentPhysicalSize)
                byteLength = currentPhysicalSize - byteOffset;

            int newPhysicalSize = currentPhysicalSize - byteLength;

            if (newPhysicalSize == 0)
            {
                SwapAnyArrayLarge(body, ReadOnlySpan<byte>.Empty, arrayIndex, _vt);
                return;
            }

            // Build combined data: before + after (skipping the removed section)
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
            }
            int afterOffset = byteOffset + byteLength;
            if (afterOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + afterOffset, currentPhysicalSize - afterOffset)
                    .CopyTo(combined.Slice(byteOffset));
            }

            SwapAnyArrayLarge(body, combined, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void RemoveFromArraySmall(BodyBase body, int arrayIndex, int byteOffset, int byteLength, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            if (byteLength % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and remove length ({byteLength}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;

            if (byteOffset >= currentPhysicalSize)
                return;

            // Clamp byteLength if it would go past the end
            if (byteOffset + byteLength > currentPhysicalSize)
                byteLength = currentPhysicalSize - byteOffset;

            int newPhysicalSize = currentPhysicalSize - byteLength;

            if (newPhysicalSize == 0)
            {
                SwapAnyArraySmall(body, ReadOnlySpan<byte>.Empty, arrayIndex, _vt);
                return;
            }

            // Build combined data: before + after (skipping the removed section)
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
            }
            int afterOffset = byteOffset + byteLength;
            if (afterOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + afterOffset, currentPhysicalSize - afterOffset)
                    .CopyTo(combined.Slice(byteOffset));
            }

            SwapAnyArraySmall(body, combined, arrayIndex, _vt);
        }

        /// <summary>
        /// Replaces a range within the array with new data.
        /// </summary>
        /// <param name="body">The body instance to modify.</param>
        /// <param name="replacement">The replacement data.</param>
        /// <param name="arrayIndex">The zero-based index of the array.</param>
        /// <param name="byteOffset">The byte offset where replacement starts.</param>
        /// <param name="byteLengthToRemove">The number of bytes to remove before inserting replacement.</param>
        public static unsafe void ReplaceInArray(BodyBase body, ReadOnlySpan<byte> replacement, int arrayIndex, int byteOffset, int byteLengthToRemove)
        {
            if (arrayIndex < 0)
                return;

            var _vt = (VectorTableHeader*)body._vTablePtr;
            if (_vt->LargeArrays)
                ReplaceInArrayLarge(body, replacement, arrayIndex, byteOffset, byteLengthToRemove, _vt);
            else
                ReplaceInArraySmall(body, replacement, arrayIndex, byteOffset, byteLengthToRemove, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReplaceInArrayLarge(BodyBase body, ReadOnlySpan<byte> replacement, int arrayIndex, int byteOffset, int byteLengthToRemove, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapLargeEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = mapEntry->ValueSize;

            if (replacement.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and replacement size ({replacement.Length}) mismatch.");
            if (byteLengthToRemove % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and remove length ({byteLengthToRemove}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = (int)mapEntry->ArrayOffset;

            // Clamp byteOffset and byteLengthToRemove
            if (byteOffset > currentPhysicalSize)
                byteOffset = currentPhysicalSize;
            if (byteOffset + byteLengthToRemove > currentPhysicalSize)
                byteLengthToRemove = currentPhysicalSize - byteOffset;

            int newPhysicalSize = currentPhysicalSize - byteLengthToRemove + replacement.Length;

            // Build combined data: before + replacement + after
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            int pos = 0;
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
                pos = byteOffset;
            }
            if (replacement.Length > 0)
            {
                replacement.CopyTo(combined.Slice(pos));
                pos += replacement.Length;
            }
            int afterOffset = byteOffset + byteLengthToRemove;
            if (afterOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + afterOffset, currentPhysicalSize - afterOffset)
                    .CopyTo(combined.Slice(pos));
            }

            SwapAnyArrayLarge(body, combined, arrayIndex, _vt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void ReplaceInArraySmall(BodyBase body, ReadOnlySpan<byte> replacement, int arrayIndex, int byteOffset, int byteLengthToRemove, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(body._data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            if (replacement.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and replacement size ({replacement.Length}) mismatch.");
            if (byteLengthToRemove % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and remove length ({byteLengthToRemove}) mismatch.");
            if (byteOffset % valueSize != 0)
                throw new ArgumentException($"Byte offset ({byteOffset}) must be aligned to value size ({valueSize}).");

            int currentPhysicalSize = mapEntry->PhysicalSize;
            int currentOffset = mapEntry->ArrayOffset;

            // Clamp byteOffset and byteLengthToRemove
            if (byteOffset > currentPhysicalSize)
                byteOffset = currentPhysicalSize;
            if (byteOffset + byteLengthToRemove > currentPhysicalSize)
                byteLengthToRemove = currentPhysicalSize - byteOffset;

            int newPhysicalSize = currentPhysicalSize - byteLengthToRemove + replacement.Length;

            // Build combined data: before + replacement + after
            Span<byte> combined = stackalloc byte[newPhysicalSize];
            int pos = 0;
            if (byteOffset > 0)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset, byteOffset).CopyTo(combined);
                pos = byteOffset;
            }
            if (replacement.Length > 0)
            {
                replacement.CopyTo(combined.Slice(pos));
                pos += replacement.Length;
            }
            int afterOffset = byteOffset + byteLengthToRemove;
            if (afterOffset < currentPhysicalSize)
            {
                new ReadOnlySpan<byte>(body._data.Ptr + currentOffset + afterOffset, currentPhysicalSize - afterOffset)
                    .CopyTo(combined.Slice(pos));
            }

            SwapAnyArraySmall(body, combined, arrayIndex, _vt);
        }
    }
}
