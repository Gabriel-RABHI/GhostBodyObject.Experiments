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

using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Structs;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegment
    {
        private byte[] _inMemoryData;
        private int _offset;
        private long _startTxnId = long.MaxValue;
        private long _endTxnId = long.MinValue;
        private SegmentHeader* _header;

        public SegmentImplementationType SegmentType { get; private set; }

        public byte* BasePointer { get; private set; }

        public bool Deletable { get; private set; }


        public static MemorySegment NewInMemory(SegmentStoreMode mode, int id, int capacity = 1024 * 1024 * 8)
        {
            return new MemorySegment(mode, SegmentImplementationType.LOHPinnedMemory, id, capacity, true, null, null, null);
        }

        private MemorySegment(SegmentStoreMode mode, SegmentImplementationType t, int id, int capacity, bool deletable = true, string? directoryPath = null, string? prefix = null, string? name = null)
        {
            if (capacity < 1024)
                throw new InvalidOperationException(nameof(capacity));
            SegmentType = t;
            Deletable = deletable;
            switch (SegmentType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    _inMemoryData = GC.AllocateUninitializedArray<byte>(capacity, pinned: true);
                    fixed (byte* p = &_inMemoryData[0])
                    {
                        BasePointer = p;
                        _header = (SegmentHeader*)p;
                    }
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    {
                        if (directoryPath == null)
                            throw new ArgumentNullException(nameof(directoryPath));
                        if (prefix == null)
                            throw new ArgumentNullException(nameof(prefix));
                        if (name == null)
                            throw new ArgumentNullException(nameof(name));
                        var path = Path.Combine(directoryPath, $"{prefix}_{name}_{id:N5}.{(Deletable ? "txn.seg" : "log.seg")}");
                        throw new NotImplementedException();
                    }
                    break;
            }
            *_header = SegmentHeader.Create(mode, id, capacity);
        }

        ~MemorySegment()
        {
            switch (SegmentType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    // -------- Nothing to do, the GC will take care of it. -------- //
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    // -------- TODO : Dispose the Memory Mapped File -------- //
                    // It does not mean that the underlying file must be deleted :
                    // - If it is not a rolling segment, the file must remain on disk : it is the case
                    //   when the segment is used for an Log, en Event Log.
                    if (Deletable)
                    {
                        // TODO : try to delete the underlying file.
                    }
                    break;
            }
        }

        public int FreeSpace => _inMemoryData.Length - _offset;

        public PinnedMemory<byte> Allocate(int size)
        {
            if (_offset + size > _inMemoryData.Length)
                throw new OverflowException();
            var memory = new PinnedMemory<byte>(_inMemoryData, _offset, size);
            _offset += size;
            return memory;
        }

        public int InsertGhost(PinnedMemory<byte> data, long txnId)
        {
            _startTxnId = Math.Min(_startTxnId, txnId);
            _endTxnId = Math.Max(_endTxnId, txnId);
            var size = data.Length;
            if (_offset + size + 4 > _inMemoryData.Length)
                throw new OverflowException();
            Write(data.Length);
            var offset = _offset;
            var memory = new PinnedMemory<byte>(_inMemoryData, _offset, size);
            data.CopyTo(memory);
            var h = memory.As<GhostHeader>();
            h->TxnId = txnId;
            h->Status = GhostStatus.Mapped;
            _offset += size;
            return offset;
        }

        public int Write<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            if (_offset + size > _inMemoryData.Length)
                throw new OverflowException();
            T* r;
            fixed (byte* p = &_inMemoryData[_offset])
            {
                r = (T*)p;
                *r = value;
            }
            int currentOffset = _offset;
            _offset += size;
            return currentOffset;
        }

        public int Write<T>(T value, out T* r) where T : unmanaged
        {
            int size = sizeof(T);
            if (_offset + size > _inMemoryData.Length)
                throw new OverflowException();
            fixed (byte* p = &_inMemoryData[_offset])
            {
                r = (T*)p;
                *r = value;
            }
            int currentOffset = _offset;
            _offset += size;
            return currentOffset;
        }
    }
}
