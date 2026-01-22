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
    public unsafe class RepositoryGhostIndex<TSegmentStore>
        where TSegmentStore : ISegmentStore
    {
        private ShortSpinLock _lock = new ShortSpinLock();
        private readonly TSegmentStore _store;
        private readonly RepositorySingleKindGhostIndex<TSegmentStore>[] _maps;

        public RepositoryGhostIndex(TSegmentStore store)
        {
            _store = store;
            _maps = new RepositorySingleKindGhostIndex<TSegmentStore>[GhostId.MAX_TYPE_COMBO];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(long bottomTxnId, SegmentReference r)
        {
            var h = _store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.AddGhost(bottomTxnId, r, h);
            } else throw new InvalidOperationException("Cannot index a missing ghost.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveGhost(SegmentReference r)
        {
            var h = _store.ToGhostHeaderPointer(r);
            if (h != null)
            {
                var map = GetIndex(h->Id.TypeCombo, true);
                map.RemoveGhost(r, h);
            } else throw new InvalidOperationException("Cannot index a missing ghost.");
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
                    } finally
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
        private readonly ISegmentStore _store;
        private readonly long _minTxnId;
        private long _maxTxnId;
        private readonly ShardedSegmentGhostMap<TSegmentStore> _map;

        public RepositorySingleKindGhostIndex(TSegmentStore store)
        {
            _store = store;
            _map = new ShardedSegmentGhostMap<TSegmentStore>(store);
        }

        public ShardedSegmentGhostMap<TSegmentStore> GhostMap => _map;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGhost(long bottomTxnId, SegmentReference r, GhostHeader* h)
        {
            _map.Set(bottomTxnId, r, h);
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
