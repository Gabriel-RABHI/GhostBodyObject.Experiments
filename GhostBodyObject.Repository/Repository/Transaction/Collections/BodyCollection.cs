using System.Collections;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Transaction;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public readonly struct BodyCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
    {
        private readonly RepositoryTransactionBodyIndex _index;

        public BodyCollection(RepositoryTransactionBodyIndex index)
        {
            _index = index;
        }

        public RepositoryTransactionBodyIndex.Enumerator<TBody> GetEnumerator()
        {
            return _index.GetEnumerator<TBody>(false);
        }

        IEnumerator<TBody> IEnumerable<TBody>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BodyWhereIterator<TBody> Where(Func<TBody, bool> predicate)
        {
            return new BodyWhereIterator<TBody>(this, predicate);
        }

        public BodyInstanceCollection<TBody> Instances => new BodyInstanceCollection<TBody>(_index, false);

        public BodyInstanceCollection<TBody> Cursor => new BodyInstanceCollection<TBody>(_index, true);

        public void ForEach(Action<TBody> action)
        {
            var enumerator = _index.GetEnumerator<TBody>(false);
            while (enumerator.MoveNext())
            {
                action(enumerator.Current);
            }
        }

        public void Scan(Action<TBody> action)
        {
            var enumerator = _index.GetEnumerator<TBody>(true);
            while (enumerator.MoveNext())
            {
                action(enumerator.Current);
            }
        }

        public int Count()
        {
            var count = 0;
            var enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                count++;
            }
            return count;
        }
    }
}
