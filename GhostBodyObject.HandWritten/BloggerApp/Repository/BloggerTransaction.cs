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
using System.Runtime.InteropServices.JavaScript;

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
            _repository.Retain(this);
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

        // --------------------------------------------------------- //
        // The Entities
        // --------------------------------------------------------- //
        #region
        #endregion
    }

    public static class BloggerUserCollection
    {
        public static void ForEach(Action<BloggerUser> action)
        {
            var enumerator = BloggerContext.Transaction.BodyIndex.GetEnumerator<BloggerUser>();
            while (enumerator.MoveNext())
            {
                action(enumerator.Current);
            }
        }

        public static void ForEachCursor(Action<BloggerUser> action)
        {
            var enumerator = BloggerContext.Transaction.BodyIndex.GetEnumerator<BloggerUser>();
            while (enumerator.MoveNext())
            {
                action(enumerator.Current);
                // !-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-!-! //
                // CAUTIONS
                // If the status of the Ghost changed during the action,
                // the body must be added to the transaction map, and a new
                // cursor body must be created.
                //
                // This ensure that a body modified during the action is correctly
                // handled.
            }
        }
    }
}
