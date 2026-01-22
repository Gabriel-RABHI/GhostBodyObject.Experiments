using GhostBodyObject.Repository.Body.Contracts;
using System.Collections;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public readonly struct BodyInstanceCollection<TBody> : IEnumerable<TBody>
       where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
    {
        private readonly RepositoryTransactionBodyIndex _index;
        private readonly bool _useCursor;

        public BodyInstanceCollection(RepositoryTransactionBodyIndex index, bool useCursor)
        {
            _index = index;
            _useCursor = useCursor;
        }

        public RepositoryTransactionBodyIndex.Enumerator<TBody> GetEnumerator()
        {
            return _index.GetEnumerator<TBody>(_useCursor);
        }

        IEnumerator<TBody> IEnumerable<TBody>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public readonly struct BodyInstanceWhereIterator<TBody> : IEnumerable<TBody>
      where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
    {
        private readonly BodyInstanceCollection<TBody> _source;
        private readonly Func<TBody, bool> _predicate;

        public BodyInstanceWhereIterator(BodyInstanceCollection<TBody> source, Func<TBody, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public IEnumerator<TBody> GetEnumerator()
        {
            foreach (var item in _source)
            {
                if (_predicate(item))
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
