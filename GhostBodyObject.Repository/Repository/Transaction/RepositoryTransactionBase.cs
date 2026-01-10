using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
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

        public RepositoryTransactionBase(GhostRepositoryBase repository, bool isReadOnly)
        {
            _repository = repository;
            _isReadOnly = isReadOnly;
            if (!isReadOnly)
            {
                _inserted = new List<GhostId>();
                _mappedMuted = new List<GhostId>();
            }
        }

        public bool IsReadOnly => _isReadOnly;

        public List<GhostId> InsertedIds => _inserted;

        public List<GhostId> MappedMutedIds => _mappedMuted;

        public volatile bool IsBusy;
    }
}
