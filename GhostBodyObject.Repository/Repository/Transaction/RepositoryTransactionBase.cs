using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Segment;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    public abstract class RepositoryTransactionBase
    {
        private readonly GhostRepositoryBase _repository;
        private readonly bool _isReadOnly;
        private List<GhostId> _inserted;
        private List<GhostId> _mappedMuted;
        private long _openingTxnId;
        private MemorySegmentStoreHolders _holders;

        public RepositoryTransactionBase(GhostRepositoryBase repository, bool isReadOnly)
        {
            _repository = repository;
            _isReadOnly = isReadOnly;
            if (!isReadOnly)
            {
                _inserted = new List<GhostId>();
                _mappedMuted = new List<GhostId>();
            }
            _holders = repository.Store.GetHolders();
            _openingTxnId = repository.CurrentTransactionId;
        }

        public bool IsReadOnly => _isReadOnly;

        public long OpeningTxnId => _openingTxnId;

        public List<GhostId> InsertedIds => _inserted;

        public List<GhostId> MappedMutedIds => _mappedMuted;

        public volatile bool IsBusy;
    }
}
