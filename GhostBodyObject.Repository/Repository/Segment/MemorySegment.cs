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
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Structs;
using System.IO.MemoryMappedFiles;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegment : IDisposable
    {
        private byte[] _inMemoryData;
        private int _offset;
        private int _capacity;
        private long _startTxnId = long.MaxValue;
        private long _endTxnId = long.MinValue;
        private SegmentHeader* _header;
        
        // MMF Fields
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _readAccessor;
        private MemoryMappedViewAccessor _writeAccessor;
        private byte* _readPtr;
        private byte* _writePtr;
        private string _filePath;

        public SegmentImplementationType SegmentType { get; private set; }

        public byte* BasePointer => _readPtr;
        public byte* WritePointer => _writePtr;

        public bool Deletable { get; private set; }
        public int Capacity => _capacity;


        public static MemorySegment NewInMemory(SegmentStoreMode mode, int id, int capacity = 1024 * 1024 * 8)
        {
            return new MemorySegment(mode, SegmentImplementationType.LOHPinnedMemory, id, capacity, true, null, null, null);
        }

        public static MemorySegment NewMemoryMapped(SegmentStoreMode mode, int id, int capacity, bool deletable, string directoryPath, string prefix, string name)
        {
            return new MemorySegment(mode, SegmentImplementationType.ProtectedMemoryMappedFile, id, capacity, deletable, directoryPath, prefix, name);
        }

        private MemorySegment(SegmentStoreMode mode, SegmentImplementationType t, int id, int capacity, bool deletable = true, string? directoryPath = null, string? prefix = null, string? name = null)
        {
            if (capacity < 1024)
                throw new InvalidOperationException(nameof(capacity));
            SegmentType = t;
            Deletable = deletable;
            _capacity = capacity;
            
            switch (SegmentType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    _inMemoryData = GC.AllocateUninitializedArray<byte>(capacity, pinned: true);
                    fixed (byte* p = &_inMemoryData[0])
                    {
                        _readPtr = p;
                        _writePtr = p;
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
                        
                        _filePath = Path.Combine(directoryPath, $"{prefix}_{name}_{id:N5}.{(Deletable ? "txn.seg" : "log.seg")}");
                        
                        _mmf = MemoryMappedFile.CreateFromFile(_filePath, FileMode.OpenOrCreate, null, capacity, MemoryMappedFileAccess.ReadWrite);
                        
                        // Create read-only view
                        _readAccessor = _mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.Read);
                        _readAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _readPtr);
                        
                        // Create write view
                        _writeAccessor = _mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.Write);
                        _writeAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _writePtr);
                        
                        _header = (SegmentHeader*)_writePtr;
                    }
                    break;
            }
            
            // Only initialize header if it's a new segment (offset 0)
            // Assuming this constructor is only called for new segments.
            // If we load existing segments, we might need logic to read the header first.
            // For now, assume creation:
            *_header = SegmentHeader.Create(mode, id, capacity);
            _offset = sizeof(SegmentHeader); // Advance offset past header
        }

        public void Dispose()
        {
             switch (SegmentType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    // GC handles array
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    if (_readAccessor != null)
                    {
                        _readAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        _readAccessor.Dispose();
                    }
                    if (_writeAccessor != null)
                    {
                         _writeAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        _writeAccessor.Dispose();
                    }
                    _mmf?.Dispose();
                    
                    if (Deletable && _filePath != null && File.Exists(_filePath))
                    {
                        try { File.Delete(_filePath); } catch { }
                    }
                    break;
            }
            GC.SuppressFinalize(this);
        }

        ~MemorySegment()
        {
            Dispose();
        }

        public int FreeSpace => _capacity - _offset;

        public PinnedMemory<byte> Allocate(int size)
        {
            if (_offset + size > _capacity)
                throw new OverflowException();
            
            // For LOH memory, we can use the array wrapper. For MMF, we need a PinnedMemory that works with pointers.
            // PinnedMemory ctor taking (byte[], offset, size) won't work for MMF if _inMemoryData is null.
            // Check PinnedMemory implementation... it seems to rely on an object owner or a pointer.
            // The existing code: new PinnedMemory<byte>(_inMemoryData, _offset, size)
            
            PinnedMemory<byte> memory;
            if (SegmentType == SegmentImplementationType.LOHPinnedMemory)
            {
                memory = new PinnedMemory<byte>(_inMemoryData, _offset, size);
            }
            else
            {
                // Assuming PinnedMemory has a constructor (object owner, void* ptr, int length)
                // We use the write pointer for allocation as we intend to write to it?
                // Or maybe read pointer? Allocate usually returns a buffer to write into.
                // But PinnedMemory is often used for reading too.
                // Let's check PinnedMemory.
                // Using 'this' as owner to prevent GC? MMF doesn't need GC pinning.
                memory = new PinnedMemory<byte>(this, _writePtr + _offset, size);
            }
            
            _offset += size;
            return memory;
        }
        
        /// <summary>
        /// Reserves space in the segment without writing data. Returns the offset where the reservation starts.
        /// </summary>
        public int Reserve(int size)
        {
            if (_offset + size > _capacity)
                throw new OverflowException();
            int current = _offset;
            _offset += size;
            return current;
        }

        public int InsertGhost(PinnedMemory<byte> data, long txnId)
        {
            _startTxnId = Math.Min(_startTxnId, txnId);
            _endTxnId = Math.Max(_endTxnId, txnId);
            var size = data.Length;
            if (_offset + size + 4 > _capacity)
                throw new OverflowException();
            
            // Write size
            *(int*)(_writePtr + _offset) = data.Length;
            _offset += 4;
            
            int offset = _offset;
            
            // Copy data
            Buffer.MemoryCopy(data.Ptr, _writePtr + _offset, size, size);
            
            // Update header in place
            var h = (GhostHeader*)(_writePtr + _offset);
            h->TxnId = txnId;
            h->Status = GhostStatus.Mapped;
            
            _offset += size;
            return offset;
        }

        public int Write<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            if (_offset + size > _capacity)
                throw new OverflowException();
            
            *(T*)(_writePtr + _offset) = value;
            
            int currentOffset = _offset;
            _offset += size;
            return currentOffset;
        }

        public int Write<T>(T value, out T* r) where T : unmanaged
        {
            int size = sizeof(T);
            if (_offset + size > _capacity)
                throw new OverflowException();
            
            T* ptr = (T*)(_writePtr + _offset);
            *ptr = value;
            r = ptr;
            
            int currentOffset = _offset;
            _offset += size;
            return currentOffset;
        }
        
        public void WriteAt<T>(int offset, T value) where T : unmanaged
        {
            if (offset + sizeof(T) > _capacity)
                throw new ArgumentOutOfRangeException(nameof(offset));
                
            *(T*)(_writePtr + offset) = value;
        }
        
        public void WriteBytesAt(int offset, byte* source, int length)
        {
             if (offset + length > _capacity)
                throw new ArgumentOutOfRangeException(nameof(offset));
             
             Buffer.MemoryCopy(source, _writePtr + offset, length, length);
        }
        
        public void Flush()
        {
            if (SegmentType == SegmentImplementationType.ProtectedMemoryMappedFile)
            {
                _writeAccessor.Flush();
            }
        }
    }
}
