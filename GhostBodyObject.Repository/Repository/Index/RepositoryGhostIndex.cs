/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Index
{
    public unsafe class RepositoryGhostIndex<TSegmentStoreProvider, TSegmentStore>
        where TSegmentStoreProvider : ISegmentStoreProvider<TSegmentStore>
        where TSegmentStore : ISegmentStore
    {
        private ShortSpinLock _lock = new ShortSpinLock();
        private TSegmentStoreProvider _store;
        private RepositorySingleKindGhostIndex<TSegmentStoreProvider, TSegmentStore>[] _maps;

        public RepositoryGhostIndex(TSegmentStoreProvider store)
        {
            _store = store;
            _maps = new RepositorySingleKindGhostIndex<TSegmentStoreProvider, TSegmentStore>[GhostId.MAX_TYPE_COMBO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(SegmentReference r)
        {
            var store = _store.GetHolders();
            var h = store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.AddGhost(store, r, h);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(SegmentReference r)
        {
            var h = (GhostHeader*)_store.GetHolders().ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.RemoveGhost(r, h);
            }
            else throw new InvalidOperationException("Cannot index a missing ghost.");
        }
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentReference FindGhost(GhostId id, long maxTxnId)
        {
            var map = GetIndex(id.TypeCombo, false);
            if (map != null)
                return map.FindGhost(_store.GetHolders(), id, maxTxnId);
            return SegmentReference.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RepositorySingleKindGhostIndex<TSegmentStoreProvider, TSegmentStore> GetIndex(GhostTypeCombo typeCombo, bool create)
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
                            typeMap = new RepositorySingleKindGhostIndex<TSegmentStoreProvider, TSegmentStore>(_store);
                            _maps[typeCombo.Value] = typeMap;
                        }
                        typeMap = _maps[typeCombo.Value];
                    }
                    finally
                    {
                        _lock.Exit();
                    }
                }
                else return null;
            }
            return typeMap;
        }
    }

    public unsafe sealed class RepositorySingleKindGhostIndex<TSegmentStoreProvider, TSegmentStore>
        where TSegmentStoreProvider : ISegmentStoreProvider<TSegmentStore>
        where TSegmentStore : ISegmentStore
    {
        private TSegmentStoreProvider _store;
        private long _minTxnId;
        private long _maxTxnId;
        private ShardedSegmentGhostMap<TSegmentStore> _map;

        public RepositorySingleKindGhostIndex(TSegmentStoreProvider store)
        {
            _store = store;
            _map = new ShardedSegmentGhostMap<TSegmentStore>();
        }

        public ShardedSegmentGhostMap<TSegmentStore> GhostMap => _map;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(TSegmentStore store, SegmentReference r, GhostHeader* h)
        {
            _map.Set(store, r, h);
            _maxTxnId = Math.Max(_maxTxnId, h->TxnId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(TSegmentStore store, SegmentReference r, GhostHeader* h)
        {
            _map.Remove(store, h->Id, h->TxnId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SegmentReference FindGhost(TSegmentStore store, GhostId id, long maxTxnId)
        {
            if (_map.Get(store, id, maxTxnId, out var r))
                return r;
            return SegmentReference.Empty;
        }
    }
}
