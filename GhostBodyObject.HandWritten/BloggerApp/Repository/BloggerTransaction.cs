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
        private int _mapIndex = 0;
        private int _indexInMap = 0;

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


        public BloggerTransaction(BloggerRepository repository, bool readOnly = false) : base(repository, readOnly)
        {
            _repository = repository;
            _bloggerUserMap = new ShardedTransactionBodyMap<BloggerUser>();
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
                            if (_txn._bloggerUserMap.Count == 0)
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                    {
                                        var body = new BloggerUser(ghost, true, true);
                                        _txn._bloggerUserMap.Set(body);
                                        action(body);
                                    }
                                }
                            }
                            else
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    var body = _txn._bloggerUserMap.Get(ghost.As<GhostHeader>()->Id, out var exist);
                                    if (exist)
                                    {
                                        if (body.Status != GhostStatus.MappedDeleted)
                                            action(body);
                                    }
                                    else
                                    {
                                        if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                        {
                                            body = new BloggerUser(ghost, true, true);
                                            _txn._bloggerUserMap.Set(body);
                                            action(body);
                                        }
                                    }
                                }
                            }
                        } else
                        {
                            // -------- No Map of this type -------- //
                        }
                    }
                    else
                    {
                        if (map != null)
                        {
                            if (_txn._bloggerUserMap.Count == 0)
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                    {
                                        var body = new BloggerUser(ghost, true, true);
                                        _txn._bloggerUserMap.Set(body);
                                        action(body);
                                    }
                                }
                            }
                            else
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    var body = _txn._bloggerUserMap.Get(ghost.As<GhostHeader>()->Id, out var exist);
                                    if (exist)
                                    {
                                        if (body.Status != GhostStatus.MappedDeleted)
                                            action(body);
                                    }
                                    else
                                    {
                                        if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                        {
                                            body = new BloggerUser(ghost, true, true);
                                            _txn._bloggerUserMap.Set(body);
                                            action(body);
                                        }
                                    }
                                }
                            }
                            foreach (var id in _txn._bloggerUserMap.InsertedIds)
                            {
                                var body = _txn._bloggerUserMap.Get(id, out var exist);
                                if (exist)
                                {
                                    action(body);
                                }
                            }
                        }
                        else
                        {
                            if (_txn._bloggerUserMap.Count != 0)
                            {
                                foreach (var body in _txn._bloggerUserMap)
                                    action(body);
                            }
                        }
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
                    else
                    {

                    }
                }
            }
        }
        #endregion
    }

    public static class BloggerUserCollection
    {
        public static void ForEach(Action<BloggerUser> action)
        {
            BloggerContext.Transaction.BloggerUserCollection.ForEach(action);
        }

        public static void ForEachCursor(Action<BloggerUser> action)
        {
            BloggerContext.Transaction.BloggerUserCollection.ForEachCursor(action);
        }
    }
}
