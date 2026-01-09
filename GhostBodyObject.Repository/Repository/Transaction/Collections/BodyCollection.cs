using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Repository.Transaction.Index;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public ref struct BodyCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        private ShardedTransactionBodyMap<TBody> _map;

        public BodyCollection(ShardedTransactionBodyMap<TBody> map)
        {
            _map = map;
        }

        public IEnumerator<TBody> GetEnumerator() => _map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

        public IEnumerable<TBody> Filter(Func<TBody, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TBody> Scan(Action<TBody> action)
        {
            throw new NotImplementedException();
        }
    }
}
