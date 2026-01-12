using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Segment;
using Xunit;

namespace GhostBodyObject.Repository.Tests.Repository.Segment
{
    public class MemorySegmentStoreShould
    {
        [Fact]
        public void AutoResizeWhenAddingMoreSegmentsThanInitialCapacity()
        {
            var store = new MemorySegmentStore(SegmentStoreMode.InMemoryRepository);

            // Add enough segments to trigger resize
            for (int i = 0; i < 1024; i++)
            {
                store.CreateSegment(1024);
            }
        }
    }
}
