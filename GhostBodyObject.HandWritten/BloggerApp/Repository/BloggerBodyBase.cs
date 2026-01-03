using GhostBodyObject.Common.Memory;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Transaction;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    // ---------------------------------------------------------
    // 3. The Base Entity
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
    public abstract class BloggerBodyBase : IEntityBody
    {
        [FieldOffset(0)]
        protected BloggerTransaction _ownerTransaction;

        [FieldOffset(8)]
        protected IntPtr _vTablePtr;

        [FieldOffset(16)]
        protected PinnedMemory<byte> _data;

        public BloggerTransaction Transaction => _ownerTransaction;

        protected BloggerBodyBase()
        {
            var current = BloggerContext.Transaction;
            if (current == null)
                throw new InvalidOperationException("Must be created in a transaction."); // Option B: _ownerToken = new GhostContext();
            else
                _ownerTransaction = current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected BloggerGhostWriteLock GuardWriteScope()
        {
            var current = BloggerContext.Transaction;
            if (_ownerTransaction != current)
                ThrowContextMismatch();
            return new BloggerGhostWriteLock(_ownerTransaction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void GuardLocalScope()
        {
            if (_ownerTransaction != BloggerContext.Transaction)
                ThrowContextMismatch();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowContextMismatch() => throw new InvalidOperationException("Cross-Context Violation.");


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
        /// <param name="src">A span containing the new data to copy into the target array. The length of this buffer must match
        /// the expected size for the array at the specified index.</param>
        /// <param name="arrayIndex">The zero-based index of the array to be updated.</param>
        public unsafe void SwapAnyArray(ReadOnlySpan<byte> src, int arrayIndex)
        {
            // This method must move arrays taking care of alignements and offsets.
            // Arrays are sorted from largest to smallest value size: [8, 4, 4, 2, 2, 1, 1]
            // Key insight: Once aligned to the next array's value size, all subsequent arrays
            // are naturally aligned (since value sizes are decreasing).
            // This allows a single block copy for all subsequent arrays.
            var _vt = (VectorTableHeader*)_vTablePtr;
            if (_vt->LargeArrays)
            {
                SwapAnyArrayLarge(src, arrayIndex, _vt);
            }
            else
            {
                SwapAnyArraySmall(src, arrayIndex, _vt);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AlignTo(int offset, int alignment)
        {
            if (alignment <= 1) return offset;
            int mask = alignment - 1;
            return (offset + mask) & ~mask;
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SwapAnyArrayLarge(ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapLargeEntry* mapBase = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset);
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
                    Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
                return;
            }

            // -------- Size changed: need to shift subsequent arrays --------
            int oldTotalSize = TotalSize;
            int newArrayEnd = currentOffset + src.Length;
            bool hasSubsequentArrays = arrayIndex + 1 < arrayCount;

            if (!hasSubsequentArrays)
            {
                // Last array: just resize and copy
                if (src.Length > currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref _data, newArrayEnd))
                {
                    mapBase = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }
                
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }
                
                if (src.Length < currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref _data, newArrayEnd))
                {
                    mapBase = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset);
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
                        Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }
                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                return;
            }

            int newTotalSize = oldTotalSize + delta;

            if (delta > 0)
            {
                // -------- Growing: resize first, then move tail backward (from end) --------
                if (TransientGhostMemoryAllocator.Resize(ref _data, newTotalSize))
                {
                    mapBase = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }
                // Move all subsequent arrays in one block (use memmove semantics for overlapping)
                if (tailLength > 0)
                {
                    Buffer.MemoryCopy(_data.Ptr + oldNextOffset, _data.Ptr + newNextOffset, tailLength, tailLength);
                }
            }
            else
            {
                // -------- Shrinking: move tail forward first, then resize --------
                if (tailLength > 0)
                    Buffer.MemoryCopy(_data.Ptr + oldNextOffset, _data.Ptr + newNextOffset, tailLength, tailLength);
                if (TransientGhostMemoryAllocator.Resize(ref _data, newTotalSize))
                {
                    mapBase = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset);
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
                    Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
            }

            mapEntry->ArrayLength = (uint)(src.Length / valueSize);
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void SwapAnyArraySmall(ReadOnlySpan<byte> src, int arrayIndex, VectorTableHeader* _vt)
        {
            ArrayMapSmallEntry* mapBase = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset);
            ArrayMapSmallEntry* mapEntry = mapBase + arrayIndex;
            int valueSize = (int)mapEntry->ValueSize;

            // Check that src length is a multiple of value size
            if (src.Length % valueSize != 0)
                throw new ArrayTypeMismatchException($"Array value size ({valueSize}) and source array size ({src.Length}) mismatch.");

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
                    Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
                return;
            }

            // -------- Size changed: need to shift subsequent arrays --------
            int oldTotalSize = TotalSize;
            int newArrayEnd = currentOffset + src.Length;
            bool hasSubsequentArrays = arrayIndex + 1 < arrayCount;

            if (!hasSubsequentArrays)
            {
                // Last array: just resize and copy
                if (src.Length > currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref _data, newArrayEnd))
                {
                        mapBase = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset);
                        mapEntry = mapBase + arrayIndex;
                }
                
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }
                
                if (src.Length < currentPhysicalSize && TransientGhostMemoryAllocator.Resize(ref _data, newArrayEnd))
                {
                        mapBase = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset);
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

            if (delta == 0)
            {
                // No shift needed, just copy new data
                if (src.Length > 0)
                {
                    fixed (byte* srcPtr = src)
                    {
                        Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                    }
                }
                mapEntry->ArrayLength = (uint)(src.Length / valueSize);
                return;
            }

            int newTotalSize = oldTotalSize + delta;

            if (delta > 0)
            {
                // -------- Growing: resize first, then move tail backward (from end) --------
                if (TransientGhostMemoryAllocator.Resize(ref _data, newTotalSize))
                {
                    mapBase = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }

                // Move all subsequent arrays in one block (use memmove semantics for overlapping)
                if (tailLength > 0)
                {
                    Buffer.MemoryCopy(_data.Ptr + oldNextOffset, _data.Ptr + newNextOffset, tailLength, tailLength);
                }
            }
            else
            {
                // -------- Shrinking: move tail forward first, then resize --------
                if (tailLength > 0)
                {
                    Buffer.MemoryCopy(_data.Ptr + oldNextOffset, _data.Ptr + newNextOffset, tailLength, tailLength);
                }
                if (TransientGhostMemoryAllocator.Resize(ref _data, newTotalSize))
                {
                    mapBase = (ArrayMapSmallEntry*)(_data.Ptr + _vt->ArrayMapOffset);
                    mapEntry = mapBase + arrayIndex;
                }
            }

            // Update all subsequent array offsets with uniform delta
            for (int i = arrayIndex + 1; i < arrayCount; i++)
            {
                ArrayMapSmallEntry* entry = mapBase + i;
                entry->ArrayOffset = (ushort)(entry->ArrayOffset + delta);
            }

            // Copy new data
            if (src.Length > 0)
            {
                fixed (byte* srcPtr = src)
                {
                    Unsafe.CopyBlockUnaligned(_data.Ptr + currentOffset, srcPtr, (uint)src.Length);
                }
            }

            mapEntry->ArrayLength = (uint)(src.Length / valueSize);
        }
    }
}
