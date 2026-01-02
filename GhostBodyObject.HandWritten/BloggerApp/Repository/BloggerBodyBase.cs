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
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
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
        /// shifted to accommodate the change. Callers should ensure that the source buffer is valid and that the index
        /// refers to an existing array.</remarks>
        /// <param name="src">A memory buffer containing the new data to copy into the target array. The length of this buffer must match
        /// the expected size for the array at the specified index.</param>
        /// <param name="arrayIndex">The zero-based index of the array to be updated.</param>
        protected unsafe void SwapAnyArray(Memory<byte> src, int arrayIndex)
        {
            // This method must move arrays taking care of alignements and offsets
            // To compute alignement, the arrays are sorted from largest to smallest entries values
            // Aligne the next array to next array value lenght, align all the nexts arrays correctly
            var _vt = (VectorTableHeader*)_vTablePtr;
            if (_vt->LargeArrays)
            {
                ArrayMapLargeEntry* mapEntry = (ArrayMapLargeEntry*)(_data.Ptr + _vt->ArrayMapOffset + (arrayIndex * sizeof(ArrayMapLargeEntry)));
                if (mapEntry->PhysicalSize != src.Length)
                {
                    // -------- Fit with actual reservation --------
                    // if the lenght is 0, return
                    // if not, copy the data at the current offset
                }
                else
                {
                    // compute the array size difference
                    var diff = 0;

                    if (diff > 0)
                    {
                        // -------- New data larger
                        // resize buffer
                        // update offsets in map entries
                        // move the next arrays at end
                        // copy new data
                        TransientGhostMemoryAllocator.Resize(ref _data, TotalSize + diff);
                    } else
                    {
                        // -------- New data shorter
                        // shift next arrays back
                        // update offsets in map entries
                        // copy new data
                        TransientGhostMemoryAllocator.Resize(ref _data, TotalSize + diff);
                    }
                }
            }
            else
            {

            }
        }
    }
}
