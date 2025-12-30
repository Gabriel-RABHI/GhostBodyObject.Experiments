using GhostBodyObject.Repository.Repository.Constants;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed unsafe class MemorySegment : IDisposable
    {
        public SegmentImplementationType SegmentType { get; private set; }

        public byte* BasePointer { get; private set; }

        public MemorySegment(SegmentImplementationType t, int id, int capacity)
        {
            SegmentType = t;
        }

        public void Dispose()
        {
        }
    }
}
