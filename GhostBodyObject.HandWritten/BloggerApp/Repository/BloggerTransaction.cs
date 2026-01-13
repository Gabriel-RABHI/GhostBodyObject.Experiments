using GhostBodyObject.HandWritten.BloggerApp.Entities.Post;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction;
using GhostBodyObject.Repository.Repository.Transaction.Collections;
using GhostBodyObject.Repository.Repository.Transaction.Index;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    public class BloggerTransaction : RepositoryTransactionBase, IModifiedBodyStream
    {
        private BloggerRepository _repository;
        private bool _closed;
        private int _mapIndex = 0;
        private int _indexInMap = 0;

        public BloggerRepository Repository => _repository;

        public void Commit()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot commit a read-only transaction.");
            if (_closed)
                throw new InvalidOperationException("Cannot commit a closed transaction.");

            _repository.CommitTransaction(this);
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
        }


        public BloggerTransaction(BloggerRepository repository, bool readOnly = false) : base(repository, readOnly)
        {
            _repository = repository;
            _bloggerUserMap = new ShardedTransactionBodyMap<BloggerUser>();
            _repository.Retain(this);
        }

        ~BloggerTransaction()
        {
            Close();
            _repository.Forget(this);
        }

        // --------------------------------------------------------- //
        // Streaming For Transaction
        // --------------------------------------------------------- //
        public void ReadModifiedBodies(Action<BodyBase> reader)
        {
            _bloggerUserMap.ReadModifiedBodies(reader);
        }

        // --------------------------------------------------------- //
        // The Entities
        // --------------------------------------------------------- //
        #region
        private ShardedTransactionBodyMap<BloggerUser> _bloggerUserMap;

        public void RegisterBody(BloggerUser body)
        {
            if (body.Inserted)
                _bloggerUserMap.InsertedIds.Add(body.Id);
            if (body.MappedDeleted || body.MappedModified)
                _bloggerUserMap.MappedMutedIds.Add(body.Id);
            _bloggerUserMap.Set(body);
        }

        public void RemoveBody(BloggerUser body)
        {
            _bloggerUserMap.Remove(body.Id);
            _bloggerUserMap.InsertedIds.Remove(body.Id);
        }

        public BloggerUserTxnCollection BloggerUserCollection
            => new BloggerUserTxnCollection(this);

        public struct BloggerUserTxnCollection : IEnumerable<BloggerUser>
        {
            private BloggerTransaction _txn;

            public BloggerUserTxnCollection(BloggerTransaction txn)
            {
                _txn = txn;
            }

            public IEnumerator<BloggerUser> GetEnumerator() => throw new NotImplementedException();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException();

            public IEnumerable<BloggerUser> Filter(Func<BloggerUser, bool> predicate)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<BloggerUser> Scan(Action<BloggerUser> action)
            {
                throw new NotImplementedException();
            }

            public BloggerUser Retreive(GhostId id)
            {
                throw new NotImplementedException();
            }

            public void ForEach(Action<BloggerUser> action)
            {
                unsafe
                {
                    var map = _txn.Repository.GhostIndex.GetIndex(BloggerUser.TypeCombo, false);
                    if (_txn.IsReadOnly)
                    {
                        if (map != null)
                        {
                            foreach (var segmentReference in map.GhostMap)
                            {
                                var ghost = _txn.Repository.Store.ToGhostHeaderPointer(segmentReference);
                                var body = _txn._bloggerUserMap.Get(ghost->Id, out var exist);
                                if (exist)
                                {
                                    action(body);
                                }
                                else
                                {
                                    body = new BloggerUser(_txn.Repository.Store.ToGhost(segmentReference), true, true);
                                    action(body);
                                }
                            }
                        }
                    }
                    else
                    {

                    }
                }
            }

            public void ForEachCursor(Action<BloggerUser> action)
            {
                unsafe
                {
                    var map = _txn.Repository.GhostIndex.GetIndex(BloggerUser.TypeCombo, false);
                    if (_txn.IsReadOnly)
                    {
                        if (map != null)
                        {
                            BloggerUser body = null;
                            foreach (var segmentReference in map.GhostMap)
                            {
                                var ghost = _txn.Repository.Store.ToGhost(segmentReference);
                                if (body == null)
                                    body = new BloggerUser(ghost, true, true);
                                else
                                    body.SwapGhost(ghost);
                                action(body);
                            }
                        }
                    }
                    else
                    {

                    }
                }
            }
        }
        #endregion
    }
}
