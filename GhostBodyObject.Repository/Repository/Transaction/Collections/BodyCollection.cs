using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction.Index;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public struct BodyCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        private ShardedTransactionBodyMap<TBody> _map;
        private RepositoryGhostIndex<MemorySegmentStore> _store;

        public int Count => _map.Count;

        public BodyCollection(ShardedTransactionBodyMap<TBody> map, RepositoryGhostIndex<MemorySegmentStore> store)
        {
            _map = map;
            _store = store;
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

        public void ForEach(Action<TBody> action)
        {
            foreach (var body in _map)
            {
                action(body);
            }
        }
    }
}
