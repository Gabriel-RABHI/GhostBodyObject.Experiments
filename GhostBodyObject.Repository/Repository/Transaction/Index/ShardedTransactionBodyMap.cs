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

using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Collections;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Transaction.Index
{
    /// <summary>
    /// High-performance sharded hash index for transaction body objects.
    /// Distributes entries across multiple <see cref="TransactionBodyMap{TBody}"/> shards
    /// to reduce contention and improve cache locality in concurrent scenarios.
    /// </summary>
    public unsafe sealed class ShardedTransactionBodyMap<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        // Default shard count of 8 - power of 2 for fast masking
        private const int DefaultShardCount = 8;
        private readonly int _shardCount;
        private readonly int _shardMask;

        private readonly TransactionBodyMap<TBody>[] _shards;

        /// <summary>
        /// Initializes a new sharded transaction body map with the specified shard count and initial capacity.
        /// </summary>
        /// <param name="shardCount">Number of shards (must be power of 2). Default is 8.</param>
        /// <param name="totalCapacity">Total initial capacity distributed across all shards. Default is 128.</param>
        public ShardedTransactionBodyMap(int shardCount = DefaultShardCount, int totalCapacity = 128)
        {
            // Ensure shard count is power of 2
            _shardCount = PowerOf2(shardCount < 1 ? DefaultShardCount : shardCount);
            _shardMask = _shardCount - 1;
            _shards = new TransactionBodyMap<TBody>[_shardCount];

            int capPerShard = Math.Max(16, totalCapacity / _shardCount);

            for (int i = 0; i < _shardCount; i++)
            {
                _shards[i] = new TransactionBodyMap<TBody>(capPerShard);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TransactionBodyMap<TBody> GetShard(GhostId id)
            => _shards[id.ShardComputation & _shardMask];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TransactionBodyMap<TBody> GetShard(short shardComputation)
            => _shards[shardComputation & _shardMask];

        // --- CRUD OPERATIONS ---

        /// <summary>
        /// Adds or updates the entry in the appropriate shard.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TBody entry)
            => GetShard(entry.Header->Id.ShardComputation).Set(entry);

        /// <summary>
        /// Retrieves the entry with the specified Id from the appropriate shard.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBody Get(GhostId id, out bool exists)
            => GetShard(id).GetRef(id, out exists);

        /// <summary>
        /// Removes the entry with the specified Id from the appropriate shard.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(GhostId id)
            => GetShard(id).Remove(id);

        /// <summary>
        /// Gets the total count of entries across all shards.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _shardCount; i++)
                    count += _shards[i].Count;
                return count;
            }
        }

        /// <summary>
        /// Gets the total capacity across all shards.
        /// </summary>
        public int Capacity
        {
            get
            {
                int capacity = 0;
                for (int i = 0; i < _shardCount; i++)
                    capacity += _shards[i].Capacity;
                return capacity;
            }
        }

        /// <summary>
        /// Gets the number of shards.
        /// </summary>
        public int ShardCount => _shardCount;

        /// <summary>
        /// Clears all entries from all shards.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _shardCount; i++)
                _shards[i].Clear();
        }

        // --- ENUMERATOR SUPPORT ---

        /// <summary>
        /// Returns an allocation-free enumerator over all non-null entries across all shards.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ShardedEnumerator GetEnumerator() => new ShardedEnumerator(_shards);

        IEnumerator<TBody> IEnumerable<TBody>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Allocation-free enumerator struct that iterates across all shards.
        /// </summary>
        public struct ShardedEnumerator : IEnumerator<TBody>
        {
            private readonly TransactionBodyMap<TBody>[] _shards;
            private int _shardIndex;
            private TransactionBodyMap<TBody>.Enumerator _currentEnum;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal ShardedEnumerator(TransactionBodyMap<TBody>[] shards)
            {
                _shards = shards;
                _shardIndex = 0;
                _currentEnum = shards.Length > 0 ? shards[0].GetEnumerator() : default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_currentEnum.MoveNext())
                    return true;

                while (++_shardIndex < _shards.Length)
                {
                    _currentEnum = _shards[_shardIndex].GetEnumerator();
                    if (_currentEnum.MoveNext())
                        return true;
                }
                return false;
            }

            public TBody Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _currentEnum.Current;
            }

            object IEnumerator.Current => Current;

            public void Reset()
            {
                _shardIndex = 0;
                _currentEnum = _shards.Length > 0 ? _shards[0].GetEnumerator() : default;
            }

            public void Dispose() { }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PowerOf2(int n)
        {
            if (n < 2) return 2;
            n--; n |= n >> 1; n |= n >> 2; n |= n >> 4; n |= n >> 8; n |= n >> 16;
            return n + 1;
        }
    }
}
