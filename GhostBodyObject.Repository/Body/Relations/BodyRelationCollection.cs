using GhostBodyObject.Repository.Body.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Body.Relations
{
    public ref struct BodyRelationCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        public bool IsReadOnly => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public void Add(TBody body)
        {
            throw new NotImplementedException();
        }

        public void Remove(TBody body)
        {
            throw new NotImplementedException();
        }

        public bool Contains(TBody body)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public IEnumerator<TBody> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<TBody> SelectScan(Func<TBody, bool> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
