using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GhostBodyObject.Repository.Repository.Index
{
    public unsafe class RepositoryGhostIndex<TSegmentStore>
        where TSegmentStore : ISegmentStore
    {
        private ShortSpinLock _lock = new ShortSpinLock();
        private TSegmentStore _store;
        private RepositorySingleKindGhostIndex<TSegmentStore>[] _maps;
        
        public RepositoryGhostIndex(TSegmentStore store)
        {
            _store = store;
            _maps = new RepositorySingleKindGhostIndex<TSegmentStore>[GhostId.MAX_TYPE_COMBO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(SegmentReference r)
        {
            var h = (GhostHeader*)_store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.AddGhost(r, h);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(SegmentReference r)
        {
            var h = (GhostHeader*)_store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.RemoveGhost(r, h);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentReference FindGhost(GhostId id, long maxTxnId)
        {
            var map = GetIndex(id.TypeCombo, true);
            if (map != null)
                return map.FindGhost(id, maxTxnId);
            return SegmentReference.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RepositorySingleKindGhostIndex<TSegmentStore> GetIndex(GhostTypeCombo typeCombo, bool create)
        {
            var typeMap = _maps[typeCombo.Value];
            if (typeMap == null)
            {
                if (create)
                {
                    _lock.Enter();
                    try
                    {
                        if (_maps[typeCombo.Value] == null)
                        {
                            typeMap = new RepositorySingleKindGhostIndex<TSegmentStore>(_store);
                            _maps[typeCombo.Value] = typeMap;
                        }
                        typeMap = _maps[typeCombo.Value];
                    }
                    finally
                    {
                        _lock.Exit();
                    }
                } else return null;
            }
            return typeMap;
        }
    }

    public unsafe sealed class RepositorySingleKindGhostIndex<TSegmentStore>
        where TSegmentStore : ISegmentStore
    {
        private ISegmentStore _store;
        private long _minTxnId;
        private long _maxTxnId;
        private ShardedSegmentGhostMap<TSegmentStore> _map;

        public RepositorySingleKindGhostIndex(TSegmentStore store)
        {
            _store = store;
            _map = new ShardedSegmentGhostMap<TSegmentStore>(store);
        }

        public ShardedSegmentGhostMap<TSegmentStore> GhostMap => _map;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(SegmentReference r, GhostHeader* h)
        {
            _map.Set(r, h);
            _maxTxnId = Math.Max(_maxTxnId, h->TxnId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(SegmentReference r, GhostHeader* h)
        {
            _map.Remove(h->Id, h->TxnId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentReference FindGhost(GhostId id, long maxTxnId)
        {
            if (_map.Get(id, maxTxnId, out var r))
                return r;
            return SegmentReference.Empty;
        }
    }
}
