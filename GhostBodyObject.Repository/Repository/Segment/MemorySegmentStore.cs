using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Helpers;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Segment
{

    public sealed unsafe class MemorySegmentStore : ISegmentStore
    {
        private SegmentStoreMode _storeMode;
        private SegmentImplementationType _implementationType;
        private bool _isPersistent = false;
        private bool _isCompactable = false;

        private MemorySegmentHolder[] _segmentHolders;
        private MemorySegmentHolder _currentHolder;
        private byte*[] _segmentPointers;
        private int _lastSegmentId = 0;
        private int _segmentCount = 0;

        public SegmentImplementationType ImplementationType => _implementationType;

        public SegmentStoreMode StoreMode { get; private set; }

        public bool IsPersistant => _isPersistent;

        public MemorySegmentStore(SegmentStoreMode mode)
        {
            _storeMode = mode;
            _implementationType = mode.ImplementationMode();
            _isPersistent = mode.IsPersistent();
            _isCompactable = mode.IsCompactable();

            _segmentHolders = new MemorySegmentHolder[2];
            _segmentPointers = new byte*[2];
        }

        /// <summary>
        /// Called when adding a reference to a segment in the Ghost Map
        /// </summary>
        /// <param name="segmentId"></param>
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
            _segmentHolders = newHolders;
        }

        /// <summary>
        /// Gets an array containing all current memory segment holders. Is used by Transactions to keep segments alive.
        /// </summary>
        /// <returns>An array of <see cref="MemorySegmentHolder"/> objects representing the current memory segment holders. The
        /// array may be empty if no holders are present.</returns>
        public MemorySegmentStoreHolders GetHolders()
        {
            return new MemorySegmentStoreHolders(_segmentHolders);
        }

        public void UpdateHolders(long bottomTxnId, long topTxnId)
        {
            // TODO : remove segments that are holding only ghosts from transactions older than bottomTxnId
        }

        public int CreateSegment(int capacity)
        {
            MemorySegment segment = null;
            switch (_implementationType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    segment = MemorySegment.NewInMemory(_storeMode, _lastSegmentId, capacity);
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    throw new NotImplementedException();
            }
            if (segment == null)
                throw new InvalidOperationException("Segment creation failed.");
            return AddSegment(segment);
        }

        private int AddSegment(MemorySegment segment)
        {
            int segmentId = _lastSegmentId;

            if (segmentId >= _segmentHolders.Length)
            {
                Array.Resize(ref _segmentHolders, _segmentHolders.Length * 2);
                var newPointers = new byte*[_segmentPointers.Length * 2];
                Array.Copy(_segmentPointers, newPointers, _segmentPointers.Length);
                _segmentPointers = newPointers;
            }

            _currentHolder = new MemorySegmentHolder(segment, segmentId);
            _segmentHolders[segmentId] = _currentHolder;
            _segmentPointers[segmentId] = segment.BasePointer;
            _lastSegmentId++;
            _segmentCount++;
            return segmentId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            => (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
            var h = ToGhostHeaderPointer(reference);
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b-1));
        }

        public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
        {
            var neededSpace = GetSize.Of8Aligned<StoreTransactionRecordHeader>(GetSize.Of(ghost));
            if (_currentHolder == null || _currentHolder.Segment.FreeSpace < GetSize.Of8Aligned<StoreTransactionRecordHeader>(GetSize.Of(ghost)))
            {
                CreateSegment(SegmentSizeComputation.GetNextSegmentSize(_storeMode, _segmentCount));
            }
            var offset = _currentHolder.Segment.InsertGhost(ghost, txnId);
            return new SegmentReference() { 
                SegmentId = (uint)_currentHolder.Index,
                Offset = (uint)offset
            };
        }
    }
}
