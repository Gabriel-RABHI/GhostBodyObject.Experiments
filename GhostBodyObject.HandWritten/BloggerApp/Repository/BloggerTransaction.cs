using GhostBodyObject.HandWritten.BloggerApp.Entities.Post;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Repository.Transaction;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    public class BloggerTransaction : RepositoryTransaction
    {
        public BloggerRepository Repository { get; }

        public void Commit()
        {

        }

        public void Rollback()
        {

        }

        public void Close()
        {

        }


        public BloggerTransaction(BloggerRepository repository, bool readOnly = false) : base(repository, readOnly)
        {
            Repository = repository;
        }

        // --------------------------------------------------------- //
        // The Entities
        // --------------------------------------------------------- //
        public IEnumerable<BloggerUser> UserCollection => new BloggerUser[0];

        public IEnumerable<BloggerPost> PostCollection => new BloggerPost[0];
    }
}
