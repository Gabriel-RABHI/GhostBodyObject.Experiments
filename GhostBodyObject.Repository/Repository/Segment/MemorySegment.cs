using GhostBodyObject.Repository.Repository.Constants;
using System;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegment
    {
        private byte[] _inMemoryData;
        private int _offset;

        public SegmentImplementationType SegmentType { get; private set; }

        public byte* BasePointer { get; private set; }

        public bool Deletable { get; private set; }


        public static MemorySegment NewInMemory(int id, int capacity = 1024 * 1024 * 8)
        {
            return new MemorySegment(SegmentImplementationType.LOHPinnedMemory, id, capacity, true, null, null, null);
        }

        private MemorySegment(SegmentImplementationType t, int id, int capacity, bool deletable = true, string? directoryPath = null, string? prefix = null, string? name = null)
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

        public int Write<T>(T value) where T : unmanaged
        {
            int size = sizeof(T);
            if (_offset + size > _inMemoryData.Length)
                throw new OverflowException();
            fixed (byte* p = &_inMemoryData[_offset])
            {
                *(T*)p = value;
            }
            int currentOffset = _offset;
            _offset += size;
            return currentOffset;
        }
    }
}
