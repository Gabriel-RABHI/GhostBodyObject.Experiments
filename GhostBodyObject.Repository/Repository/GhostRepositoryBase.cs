using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction;
using GhostBodyObject.Repository.Repository.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using GhostBodyObject.Repository.Repository.Structs;

namespace GhostBodyObject.Repository.Repository
{
    public class GhostRepositoryBase
    {
        private object _locker = new object();

        public delegate void TransactionCommiter(ref StoreTransactionWriter writer);

        /// <summary>
        /// The underlying Memory Segment Store where Ghost data is stored.
        /// </summary>
        private readonly MemorySegmentStore _store;

        /// <summary>
        /// The ghost index that reference memory segment.
        /// </summary>
        private readonly RepositoryGhostIndex<MemorySegmentStore> _ghostIndex;

        /// <summary>
        /// Represents the range of transaction IDs associated with the current repository transaction.
        /// </summary>
        private GhostRepositoryTransactionIdRange _transactionRange = new GhostRepositoryTransactionIdRange();

        #region PROPERTIES
        public long CurrentTransactionId => _transactionRange.CurrentTransactionId;

        public RepositoryGhostIndex<MemorySegmentStore> GhostIndex => _ghostIndex;

        public MemorySegmentStore Store => _store;
        #endregion

        public void CommitTransaction(TransactionCommiter commiter)
        {
            lock (_locker)
            {
                var writer = new StoreTransactionWriter()
                {
                    Repository = this,
                    Store = _store,
                    TransactionId = _transactionRange.CurrentTransactionId
                };
                writer.OpenTransaction();
                commiter(ref writer);
                writer.CloseTransaction();
                _transactionRange.IncrementCurrentTransactionId();
            }
        }

        public GhostRepositoryBase(SegmentStoreMode mode = SegmentStoreMode.InMemoryRepository, string path = default)
        {
            _store = new MemorySegmentStore(mode);
            _ghostIndex = new RepositoryGhostIndex<MemorySegmentStore>(_store);
        }

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

    public struct StoreTransactionWriter
    {
        public GhostRepositoryBase Repository { get; init; }

        public MemorySegmentStore Store { get; init; }

        public long TransactionId { get; init; }

        public void OpenTransaction()
        {
        }

        public void CloseTransaction()
        {
        }

        public SegmentReference StoreGhost(PinnedMemory<byte> ghost)
        {
            var r = Store.StoreGhost(ghost, TransactionId);
            Repository.GhostIndex.AddGhost(r);
            return r;
        }
    }
}
