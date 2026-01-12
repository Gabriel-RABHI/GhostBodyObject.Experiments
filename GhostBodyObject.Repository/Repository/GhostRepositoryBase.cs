using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository
{
    public class GhostRepositoryBase
    {
        private object _locker = new object();

        /// <summary>
        /// The underlying Memory Segment Store where Ghost data is stored.
        /// </summary>
        private readonly MemorySegmentStore _store;
        /// <summary>
        /// The ghost index that reference memory segment.
        /// </summary>
        private readonly RepositoryGhostIndex<MemorySegmentStore> _ghostIndex;
        private GhostRepositoryTransactionIdRange _transactionRange = new GhostRepositoryTransactionIdRange();

        public long CurrentTransactionId => _transactionRange.CurrentTransactionId;

        public void CommitTransaction(Action<long> commiter)
        {
            lock (_locker)
            {
                var commitedId = _transactionRange.CurrentTransactionId;
                commiter(commitedId);
                _transactionRange.IncrementGetCurrentTransactionId();
            }
        }

        public RepositoryGhostIndex<MemorySegmentStore> GhostIndex => _ghostIndex;

        public GhostRepositoryBase(SegmentStoreMode mode = SegmentStoreMode.InMemoryRepository, string path = default)
        {
            _store = new MemorySegmentStore(mode);
            _ghostIndex = new RepositoryGhostIndex<MemorySegmentStore>(_store);
        }

        public MemorySegmentStore Store => _store;

        /// <summary>
        /// When a transaction is opened, his _openingTxnId field is the view generation of the ghost repository.
        /// The 
        /// </summary>
        /// <param name="tnx"></param>
        public void Retain(RepositoryTransactionBase tnx)
        {
            _transactionRange.IncrementTransactionViewId(tnx.OpeningTxnId);
        }

        public void Forget(RepositoryTransactionBase tnx)
        {
            if (_transactionRange.DecrementTransactionViewId(tnx.OpeningTxnId))
            {
                Store.UpdateHolders(_transactionRange.BottomTransactionId, _transactionRange.TopTransactionId);
            }
        }
    }
}
