using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    public abstract class RepositoryTransaction
    {
        private readonly GhostRepository _repository;
        private readonly bool _isReadOnly;

        public RepositoryTransaction(GhostRepository repository, bool isReadOnly)
        {
            _repository = repository;
            _isReadOnly = isReadOnly;
        }

        public bool IsReadOnly => _isReadOnly;

        public volatile bool IsBusy;
    }
}
