using System.Runtime.InteropServices.JavaScript;
using GhostBodyObject.HandWritten.BloggerApp.Entities.Post;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Structs;
using GhostBodyObject.Repository.Repository.Transaction;
using GhostBodyObject.Repository.Repository.Transaction.Collections;
using GhostBodyObject.Repository.Repository.Transaction.Index;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    public class BloggerTransaction : RepositoryTransactionBase, IModifiedBodyStream
    {
        private BloggerRepository _repository;
        private bool _closed;

        public BloggerRepository Repository => _repository;

        public void Commit(bool concurrently)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot commit a read-only transaction.");
            if (_closed)
                throw new InvalidOperationException("Cannot commit a closed transaction.");

            _repository.CommitTransaction(this, concurrently);
        }

        public void Rollback()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot rollback a read-only transaction.");
        }

        public void Close()
        {
            if (_closed)
                return;
            if (!IsReadOnly)
                Rollback();
            _repository.Forget(this);
        }


        public BloggerTransaction(BloggerRepository repository, bool readOnly = false) : base(repository, readOnly, 101)
        {
            _repository = repository;
        }

        ~BloggerTransaction()
        {
            Close();
        }

        // --------------------------------------------------------- //
        // Streaming For Transaction
        // --------------------------------------------------------- //
        public void ReadModifiedBodies(Action<BodyBase> reader)
        {
            _bodyIndex.ReadModifiedBodies(reader);
        }

        public BodyCollection<BloggerUser> Users => new BodyCollection<BloggerUser>(_bodyIndex);
    }

    public static class BloggerCollections
    {
        public static BodyCollection<BloggerUser> BloggerUsers => BloggerContext.Transaction.Users;
    }
}
