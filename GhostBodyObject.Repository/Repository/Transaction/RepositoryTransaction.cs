using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    internal class RepositoryTransaction
    {
        private readonly GhostRepository _repository;

        // TODO : to replace with a struct Enumerator to avoid allocations.
        public IEnumerable<TBody> EnumerateGhostTable<TBody>(ushort typeCombo, ulong txnId)
            where TBody : IEntityBody
        {
            // -------- For Principle -------- //
            throw new NotSupportedException();
        }

        public TBody Retreive<TBody>(GhostId id, ulong txnId)
            where TBody : IEntityBody
        {
            // -------- For Principle -------- //
            // Retreive the Ghost header.
            // if (h->Status == GhostStatus.Deleted)
            //      return null;

            // 1. Retreive the Ghost Header

            // 2. Create a Body instance from the BodyFactory : use unsafe.As() to cast the IBodyFactory into the concrete BodyFactory<TBody>.
            //    The Factory will use the Shema version from the GhostHeader to choose the right vTable version.

            throw new NotImplementedException();
        }
    }
}
