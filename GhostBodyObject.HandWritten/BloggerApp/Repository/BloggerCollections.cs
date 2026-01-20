using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Repository.Transaction.Collections;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    public static class BloggerCollections
    {
        public static BodyCollection<BloggerUser> BloggerUsers => BloggerContext.Transaction.Users;
    }
}
