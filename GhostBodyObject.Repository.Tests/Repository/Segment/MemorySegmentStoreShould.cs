using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Segment;

namespace GhostBodyObject.Repository.Tests.Repository.Segment
{
    public class MemorySegmentStoreShould
    {
        [Fact]
        public void HoldSegmentReferences()
        {
            var store = new MemorySegmentStore(SegmentImplementationType.LOHPinnedMemory);

        }
    }
}
