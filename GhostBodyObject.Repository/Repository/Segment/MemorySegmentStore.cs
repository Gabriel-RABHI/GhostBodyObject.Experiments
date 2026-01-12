using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegmentStore : ISegmentStore
    {
        private SegmentStoreMode _storeMode;
        private SegmentImplementationType _implementationType;
        private bool _isPersistant = false;

        private MemorySegmentHolder[] _segmentHolders;
        private MemorySegmentHolder _currentHolder;
        private byte*[] _segmentPointers;
        private int _lastSegmentId = 0;

        public SegmentImplementationType ImplementationType => _implementationType;

        public SegmentStoreMode StoreMode { get; private set; }

        public bool IsPersistant => _isPersistant;

        public MemorySegmentStore(SegmentStoreMode mode)
        {
            _storeMode = mode;
            switch (_storeMode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.InMemoryLog:
                    _implementationType = SegmentImplementationType.LOHPinnedMemory;
                    _isPersistant = false;
                    break;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    _implementationType = SegmentImplementationType.ProtectedMemoryMappedFile;
                    _isPersistant = true;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
            _segmentHolders = new MemorySegmentHolder[16];
            _segmentPointers = new byte*[16];
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
            if (_currentHolder == null || _currentHolder.Segment.FreeSpace < ghost.Length + 4)
            {
                CreateSegment(1024 * 1024 * 8);
            }
            var offset = _currentHolder.Segment.InsertGhost(ghost, txnId);
            return new SegmentReference() { 
                SegmentId = (uint)_currentHolder.Index,
                Offset = (uint)offset
            };
        }
    }
}
