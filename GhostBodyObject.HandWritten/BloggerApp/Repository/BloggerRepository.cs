using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction.Collections;
using GhostBodyObject.Repository.Repository.Transaction.Index;

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
