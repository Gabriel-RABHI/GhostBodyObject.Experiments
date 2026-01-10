using GhostBodyObject.HandWritten.BloggerApp.Entities.Post;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.Repository.Repository.Transaction;
using GhostBodyObject.Repository.Repository.Transaction.Collections;
using GhostBodyObject.Repository.Repository.Transaction.Index;

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
            _bloggerUserMap = new ShardedTransactionBodyMap<BloggerUser>();
        }

        // --------------------------------------------------------- //
        // The Entities
        // --------------------------------------------------------- //
        #region
        private ShardedTransactionBodyMap<BloggerUser> _bloggerUserMap;

        public void RegisterBody(BloggerUser body)
            => _bloggerUserMap.Set(body);

        public void RemoveBody(BloggerUser body)
            => _bloggerUserMap.Remove(body.Id);

        public BodyCollection<BloggerUser> BloggerUserCollection
            => new BodyCollection<BloggerUser>(_bloggerUserMap);
        #endregion
    }
}
