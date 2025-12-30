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
    internal unsafe class RepositoryGhostIndex
    {
        private ShortSpinLock _lock = new ShortSpinLock();
        private ISegmentStore _store;
        private RepositorySingleTypeGhostIndex[] _maps;
        
        public RepositoryGhostIndex(ISegmentStore store)
        {
            _store = store;
            _maps = new RepositorySingleTypeGhostIndex[GhostId.MAX_TYPE_ID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(SegmentReference r)
        {
            var h = (GhostHeader*)_store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeIdentifier, h->Id.Kind, true);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(SegmentReference r)
        {
            var h = (GhostHeader*)_store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeIdentifier, h->Id.Kind, true);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentReference FindGhost(GhostId id, long maxTxnId)
        {
            var map = GetIndex(id.TypeIdentifier, id.Kind, false);
            if (map != null)
                return map.FindGhost(id, maxTxnId);
            return SegmentReference.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RepositorySingleKindGhostIndex GetIndex(ushort typeId, GhostIdKind kind, bool create)
        {
            var typeMap = _maps[typeId];
            if (typeMap == null)
            {
                if (create)
                {
                    _lock.Enter();
                    try
                    {
                        if (_maps[typeId] == null)
                        {
                            typeMap = new RepositorySingleTypeGhostIndex(_store);
                            _maps[typeId] = typeMap;
                        }
                        typeMap = _maps[typeId];
                    }
                    finally
                    {
                        _lock.Exit();
                    }
                }
                else
                {
                    return null;
                }
            }
            return typeMap.GetKindIndex(kind, create);
        }
    }

    internal class RepositorySingleTypeGhostIndex
    {
        private ShortSpinLock _lock = new ShortSpinLock();
        private ISegmentStore _store;
        private RepositorySingleKindGhostIndex[] _byKind;

        public RepositorySingleTypeGhostIndex(ISegmentStore store)
        {
            _store = store;
            _byKind = new RepositorySingleKindGhostIndex[GhostId.MAX_KIND];
        }

        public RepositorySingleKindGhostIndex GetKindIndex(GhostIdKind kind, bool create)
        {
            var map = _byKind[(int)kind];
            if (map == null)
            {
                if (create)
                {
                    _lock.Enter();
                    try
                    {
                        if (_byKind[(int)kind] == null)
                        {
                            map = new RepositorySingleKindGhostIndex(_store);
                            _byKind[(int)kind] = map;
                        }
                        map = _byKind[(int)kind];
                    }
                    finally
                    {
                        _lock.Exit();
                    }
                }
                else
                {
                    return null;
                }
            }
            return map;
        }
    }

    internal unsafe class RepositorySingleKindGhostIndex
    {
        private ISegmentStore _store;
        private long _minTxnId;
        private long _maxTxnId;
        private SegmentGhostTransactionnalMap _map;

        public RepositorySingleKindGhostIndex(ISegmentStore store)
        {
            _store = store;
            _map = new SegmentGhostTransactionnalMap(store);
        }

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
