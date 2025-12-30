namespace GhostBodyObject.Repository.Repository.Segment
{
    internal sealed class MemorySegmentHolder {
        private int _referenceCount = 0;

        public MemorySegment Segment { get; set; }

        public int ReferenceCount => _referenceCount;

        public bool Forgotten => Segment == null;

        public MemorySegmentHolder(MemorySegment segment)
        {
            Segment = segment;
        }

        public void IncrementReferenceCount() => Interlocked.Increment(ref _referenceCount);

        public void DecrementReferenceCount()
        {
            if (Interlocked.Decrement(ref _referenceCount) == 0)
                Segment = null;
        }
    }
}
