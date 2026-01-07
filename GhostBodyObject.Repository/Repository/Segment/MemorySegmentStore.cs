using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegmentStore : ISegmentStore
    {
        private MemorySegmentHolder[] _segmentHolders;
        private byte*[] _segmentPointers;
        private int _lastSegmentId = 0;

        public SegmentImplementationType SegmentType { get; private set; }

        public MemorySegmentStore(SegmentImplementationType t)
        {
            SegmentType = t;
            _segmentHolders = new MemorySegmentHolder[64];
            _segmentPointers = new byte*[64];
        }

        public void IncrementSegmentReferenceCount(int segmentId)
        {
            _segmentHolders[segmentId].IncrementReferenceCount();
        }

        public void DecrementSegmentReferenceCount(int segmentId)
        {
            if (_segmentHolders[segmentId].DecrementReferenceCount())
            {
                RebuildSegmentHolders();
            }
        }

        public void RebuildSegmentHolders()
        {
            var newHolders = new MemorySegmentHolder[_segmentHolders.Length];
            for (int i = 0; i < _segmentHolders.Length; i++)
            {
                if (_segmentHolders[i] != null && _segmentHolders[i].ReferenceCount > 0)
                    newHolders[i] = _segmentHolders[i];
            }
        }

        /// <summary>
        /// Gets an array containing all current memory segment holders. Is used by Transactions to keep segments alive.
        /// </summary>
        /// <returns>An array of <see cref="MemorySegmentHolder"/> objects representing the current memory segment holders. The
        /// array may be empty if no holders are present.</returns>
        public MemorySegmentHolder[] GetHolders()
        {
            return _segmentHolders;
        }

        public int CreateSegment(int capacity)
        {
            var segment = new MemorySegment(SegmentType, _lastSegmentId, capacity);
            return AddSegment(segment);
        }

        public int AddSegment(MemorySegment segment)
        {
            int segmentId = _lastSegmentId;
            _segmentHolders[segmentId] = new MemorySegmentHolder(segment);
            _segmentPointers[segmentId] = segment.BasePointer;
            _lastSegmentId++;
            return segmentId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            => (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);
    }
}
