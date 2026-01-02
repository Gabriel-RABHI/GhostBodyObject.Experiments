namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed class MemorySegmentHolder {
        private int _referenceCount = 0;

        public MemorySegment Segment { get; set; }

        public int ReferenceCount => _referenceCount;

        public bool Forgotten => Segment == null;

        public MemorySegmentHolder(MemorySegment segment)
        {
            Segment = segment;
        }

        public void IncrementReferenceCount() => Interlocked.Increment(ref _referenceCount);

        public bool DecrementReferenceCount()
            => Interlocked.Decrement(ref _referenceCount) == 0;
    }
}
