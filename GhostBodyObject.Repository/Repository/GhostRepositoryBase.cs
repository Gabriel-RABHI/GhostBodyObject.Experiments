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
        private long _txnId = 0;

        public long TransactionId => _txnId;

        public void CommitTransaction(Action<long> commiter)
        {
            lock (_locker)
            {
                _txnId++;
                commiter(_txnId);
            }
        }

        public RepositoryGhostIndex<MemorySegmentStore> GhostIndex => _ghostIndex;

        public GhostRepositoryBase(SegmentImplementationType segmentType = SegmentImplementationType.LOHPinnedMemory, string path = default)
        {
            _store = new MemorySegmentStore(segmentType);
            _ghostIndex = new RepositoryGhostIndex<MemorySegmentStore>(_store);
        }

        public MemorySegmentStore Store => _store;
    }
}
