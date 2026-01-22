using GhostBodyObject.Repository.Repository;
using GhostBodyObject.Repository.Repository.Constants;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    public class BloggerRepository : GhostRepositoryBase
    {
        public BloggerRepository(SegmentStoreMode mode = SegmentStoreMode.InMemoryVolatileRepository, string path = default)
            : base(mode, path)
        {
        }
    }
}
