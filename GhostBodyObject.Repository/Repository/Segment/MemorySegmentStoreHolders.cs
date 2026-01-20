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

using GhostBodyObject.Repository.Ghost.Structs;

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

using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public unsafe class MemorySegmentStoreHolders : ISegmentStore
    {
        private MemorySegmentStore _parent;
        private MemorySegmentHolder[] _segmentHolders;
        private byte*[] _segmentPointers;

        public MemorySegmentStoreHolders(MemorySegmentStore parent, MemorySegmentHolder[] holders, byte*[] segmentPointers)
        {
            _parent = parent;
            _segmentHolders = holders;
            _segmentPointers = segmentPointers;
        }

        public MemorySegmentStore Parent => _parent;

        public MemorySegmentHolder[] Holders => _segmentHolders;

        public byte*[] Pointers => _segmentPointers;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementSegmentHolderUsage(SegmentReference reference)
        {
            var ghostH = ToGhostHeaderPointer(reference);
            var recordH = (StoreTransactionRecordHeader*)((byte*)ghostH - sizeof(StoreTransactionRecordHeader));
            long size = recordH->Size + sizeof(StoreTransactionRecordHeader);
            _segmentHolders[reference.SegmentId].IncrementUsage(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementSegmentHolderUsage(SegmentReference reference)
        {
            var ghostH = ToGhostHeaderPointer(reference);
            var recordH = (StoreTransactionRecordHeader*)((byte*)ghostH - sizeof(StoreTransactionRecordHeader));
            long size = recordH->Size + sizeof(StoreTransactionRecordHeader);
            if (_segmentHolders[reference.SegmentId].DecrementUsage(size))
            {
                _parent.RebuildSegmentHolders();
            }
        }

        public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
        {
            throw new NotImplementedException();
        }

#if DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
        {
            if (reference.SegmentId >= _segmentPointers.Length || _segmentPointers[reference.SegmentId] == null || _segmentHolders[reference.SegmentId] == null)
                return null;
            if (reference.Offset > _segmentHolders[reference.SegmentId].Segment.Capacity)
                throw new OverflowException($"The offset is superior to segment size.");

            return (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
            var h = ToGhostHeaderPointer(reference);
            // -------------- Can be null, because new object can 
            if (h == null)
                return PinnedMemory<byte>.Empty;
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b - 1));
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            => (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
            var h = ToGhostHeaderPointer(reference);
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b - 1));
        }
#endif
        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            => (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
            var h = ToGhostHeaderPointer(reference);
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b - 1));
        }*/
    }
}
