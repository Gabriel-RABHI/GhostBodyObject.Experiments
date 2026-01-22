using GhostBodyObject.Repository.Body.Contracts;
using System.Collections;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public readonly struct BodyWhereIterator<TBody> : IEnumerable<TBody>
        where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
    {
        private readonly BodyCollection<TBody> _source;
        private readonly Func<TBody, bool> _predicate;

        public BodyWhereIterator(BodyCollection<TBody> source, Func<TBody, bool> predicate)
        {
            _source = source;
            _predicate = predicate;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_source.GetEnumerator(), _predicate);
        }

        IEnumerator<TBody> IEnumerable<TBody>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public BodyInstanceWhereIterator<TBody> Instances()
        {
            return new BodyInstanceWhereIterator<TBody>(_source.Cursor, _predicate);
        }

        public struct Enumerator : IEnumerator<TBody>
        {
            private RepositoryTransactionBodyIndex.Enumerator<TBody> _sourceEnumerator;
            private readonly Func<TBody, bool> _predicate;
            private TBody _current;

            internal Enumerator(RepositoryTransactionBodyIndex.Enumerator<TBody> sourceEnumerator, Func<TBody, bool> predicate)
            {
                _sourceEnumerator = sourceEnumerator;
                _predicate = predicate;
                _current = null;
            }

            public TBody Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _sourceEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                while (_sourceEnumerator.MoveNext())
                {
                    var item = _sourceEnumerator.Current;
                    if (_predicate(item))
                    {
                        if (item.Status == Ghost.Constants.GhostStatus.Mapped)
                            item = TBody.Create(item._data, true, true);
                        _current = item;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
