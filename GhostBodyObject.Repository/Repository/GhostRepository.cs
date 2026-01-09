using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Repository
{
    public class GhostRepository
    {
        /// <summary>
        /// The underlying Memory Segment Store where Ghost data is stored.
        /// </summary>
        private readonly MemorySegmentStore _store;
        /// <summary>
        /// The ghost index that reference memory segment.
        /// </summary>
        private readonly RepositoryGhostIndex<MemorySegmentStore> _index;
        private readonly List<WeakReference<RepositoryTransaction>> _transactions;
        private readonly IBodyFactory[] _bodyFactories;

        public GhostRepository(SegmentImplementationType segmentType = SegmentImplementationType.LOHPinnedMemory, string path = default)
        {
            _store = new MemorySegmentStore(segmentType);
            _index = new RepositoryGhostIndex<MemorySegmentStore>(_store);
            _bodyFactories = new IBodyFactory[GhostId.MAX_TYPE_COMBO];
        }

        // -----------------------------------------------------------------------------
        // Almost all methods are TBody generic. They are used by each Rpository and Transaction generated code.
        // This way, we avoid boxing/unboxing and casting at runtime.
        // The GhostRepository is agnostic of concrete Body types.
        // -----------------------------------------------------------------------------

        // TODO : to replace with a struct Enumerator to avoid allocations.
        protected IEnumerable<TBody> EnumerateGhostTable<TBody>(ushort typeCombo, ulong txnId)
            where TBody : BodyBase
        {
            // -------- For Principle -------- //
            throw new NotSupportedException();
        }

        protected TBody Retreive<TBody>(GhostId id, ulong txnId)
            where TBody : BodyBase
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
