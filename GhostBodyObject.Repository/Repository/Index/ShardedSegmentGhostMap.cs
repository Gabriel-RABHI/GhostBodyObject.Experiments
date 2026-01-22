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

using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

// -----------------------------------------------------------------------------
// 1. THE SHARDED WRAPPER (Entry Point)
// -----------------------------------------------------------------------------
public unsafe sealed class ShardedSegmentGhostMap<TSegmentStore>
        where TSegmentStore : ISegmentStore
{
    // Must be Power of 2. 16 is a sweet spot for cache distribution (1024 bytes of references).
    private const int ShardCount = 16;
    private const int ShardMask = ShardCount - 1;
    // Shift amount to extract the top bits of the UpperRandomPart.
    // Assuming UpperRandomPart is ulong, we shift 60 to get top 4 bits (64 - 4 = 60).
    // If it is int/uint, shift 28 (32 - 4 = 28). Assuming ulong based on standard "GhostId".
    private const int ShiftAmount = 60;

    private readonly SegmentGhostMap<TSegmentStore>[] _shards;

    public ShardedSegmentGhostMap(TSegmentStore store, int totalCapacity = 1024)
    {
        _shards = new SegmentGhostMap<TSegmentStore>[ShardCount];
        int capPerShard = Math.Max(16, totalCapacity / ShardCount);

        for (int i = 0; i < ShardCount; i++)
        {
            _shards[i] = new SegmentGhostMap<TSegmentStore>(store, capPerShard);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SegmentGhostMap<TSegmentStore> GetShard(GhostId id) => _shards[id.ShardComputation & ShardMask];

    // --- CRUD OPERATIONS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(long bottomTxnId, SegmentReference r, GhostHeader* h)
        => GetShard(h->Id).SetAndRemove(r, h, bottomTxnId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(GhostId id, long maxTxnId, out SegmentReference r)
        => GetShard(id).Get(id, maxTxnId, out r);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(GhostId id, long txnId)
        => GetShard(id).Remove(id, txnId);

    public int Count {
        get {
            int count = 0;
            for (int i = 0; i < ShardCount; i++)
                count += _shards[i].Count;
            return count;
        }
    }

    public int Capacity {
        get {
            int capacity = 0;
            for (int i = 0; i < ShardCount; i++)
                capacity += _shards[i].Capacity;
            return capacity;
        }
    }

    // --- ENUMERATORS ---

    /// <summary>
    /// Yields ALL versions visible at maxTxnId across all shards.
    /// </summary>
    public ShardedEnumerator GetEnumerator()
        => new ShardedEnumerator(_shards);

    /// <summary>
    /// Yields ONLY the latest version of each key visible at maxTxnId.
    /// </summary>
    public ShardedDeduplicatedEnumerator GetDeduplicatedEnumerator(long maxTxnId)
        => new ShardedDeduplicatedEnumerator(_shards, maxTxnId);

    // --- STRUCT ENUMERATORS (Flattened Loops) ---

    public struct ShardedEnumerator
    {
        private readonly SegmentGhostMap<TSegmentStore>[] _shards;
        private int _shardIndex;
        private SegmentGhostMap<TSegmentStore>.Enumerator _currentEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ShardedEnumerator(SegmentGhostMap<TSegmentStore>[] shards)
        {
            _shards = shards;
            _shardIndex = 0;
            _currentEnum = shards[0].GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_currentEnum.MoveNext()) return true;

            while (++_shardIndex < _shards.Length)
            {
                _currentEnum = _shards[_shardIndex].GetEnumerator();
                if (_currentEnum.MoveNext()) return true;
            }
            return false;
        }

        public SegmentReference Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentEnum.Current;
        }
    }

    public struct ShardedDeduplicatedEnumerator
    {
        private readonly SegmentGhostMap<TSegmentStore>[] _shards;
        private readonly long _maxTxnId;
        private int _shardIndex;
        private SegmentGhostMap<TSegmentStore>.DeduplicatedEnumerator _currentEnum;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ShardedDeduplicatedEnumerator(SegmentGhostMap<TSegmentStore>[] shards, long maxTxnId)
        {
            _shards = shards;
            _maxTxnId = maxTxnId;
            _shardIndex = 0;
            _currentEnum = shards[0].GetDeduplicatedEnumerator(maxTxnId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_currentEnum.MoveNext()) return true;

            while (++_shardIndex < _shards.Length)
            {
                _currentEnum = _shards[_shardIndex].GetDeduplicatedEnumerator(_maxTxnId);
                if (_currentEnum.MoveNext()) return true;
            }
            return false;
        }

        public SegmentReference Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentEnum.Current;
        }
    }
}
