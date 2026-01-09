using GhostBodyObject.Repository.Body.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public ref struct BodyCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        public IEnumerator<TBody> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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
