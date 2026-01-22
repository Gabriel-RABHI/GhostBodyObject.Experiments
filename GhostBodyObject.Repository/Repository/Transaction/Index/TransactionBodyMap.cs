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

#undef THREAD_SAFE

using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Collections;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Transaction.Index
{
    /// <summary>
    /// High-performance hash index for transaction body objects.
    /// Uses open addressing with linear probing and stores cached random parts
    /// to minimize cache misses during lookups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Performance Optimization:</b> This implementation uses a parallel <c>_randomParts</c> array 
    /// that caches the 16-bit Tag of each GhostId's RandomPart as a <c>short</c>. During probing, 
    /// the cached random part is compared first (fast, L1-cache friendly) before dereferencing the 
    /// body pointer.
    /// </para>
    /// </remarks>
    public unsafe class TransactionBodyMap<TBody>
        where TBody : BodyBase
    {
        // Parallel arrays for better cache locality during probing
        // Using short (2 bytes) for memory efficiency
        // Correctness is guaranteed by full GhostId verification on match
        private short[] _randomParts;  // Cached 16-bit GhostId.RandomPartTag
        private TBody[] _entries;
        private int _count;
        private int _capacity;
        private int _mask;
        private int _resizeThreshold;
        private int _shrinkThreshold;

        private const float LoadFactor = 0.75f;
        private const float ShrinkFactor = 0.25f;
        private const int InitialCapacity = 16;

#if THREAD_SAFE
        private ShortSpinLock _lock;
#endif

        public int Count => _count;

        public int Capacity => _capacity;

        public TransactionBodyMap(int initialCapacity = InitialCapacity)
        {
            if (initialCapacity < InitialCapacity) initialCapacity = InitialCapacity;
            _capacity = PowerOf2(initialCapacity);
            _mask = _capacity - 1;
            _entries = new TBody[_capacity];
            _randomParts = new short[_capacity];

            UpdateThresholds();
        }

        public void Release()
        {
            //Clear();
        }

        /// <summary>
        /// Adds or Updates the entry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TBody entry)
        {
#if THREAD_SAFE
      _lock.Enter();
      try
            {
#endif
            if (_count >= _resizeThreshold)
                Resize(_capacity * 2);

            // Cache the ID once - avoid repeated pointer dereferences
            var entryId = entry.Header->Id;
            short entryTag = entryId.RandomPartTag;
            int index = entryId.SlotComputation & _mask;

            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            while (true)
            {
                if (entries[index] == null)
                {
                    entries[index] = entry;
                    randomParts[index] = entryTag;
                    _count++;
                    return;
                }

                // Fast filter: compare cached random parts first (16-bit, excellent cache density)
                // Then verify full ID match to handle RandomPart collisions
                if (randomParts[index] == entryTag && entries[index].Header->Id == entryId)
                {
                    entries[index] = entry;
                    return;
                }

                index = (index + 1) & mask;
            }
#if THREAD_SAFE
            }
      finally
            {
    _lock.Exit();
            }
#endif
        }

        /// <summary>
        /// Retrieves the entry with the specified Id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBody GetRef(GhostId id, out bool exists)
        {
#if THREAD_SAFE
        _lock.Enter();
            try
            {
#endif
            short idTag = id.RandomPartTag;
            int index = id.SlotComputation & _mask;

            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            while (true)
            {
                TBody current = entries[index];
                if (current == null)
                {
                    exists = false;
                    return null;
                }
                // Fast filter + full verification for correctness
                if (randomParts[index] == idTag && current.Header->Id == id)
                {
                    exists = true;
                    return current;
                }
                index = (index + 1) & mask;
            }
#if THREAD_SAFE
       }
            finally
            {
  _lock.Exit();
            }
#endif
        }

        /// <summary>
        /// Removes the entry with the specified Id.
        /// Shrinks the internal array if usage drops below 25%.
        /// </summary>
        public bool Remove(GhostId id)
        {
#if THREAD_SAFE
       _lock.Enter();
     try
        {
#endif
            short idTag = id.RandomPartTag;
            int i = id.SlotComputation & _mask;

            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            while (true)
            {
                TBody entry = entries[i];
                if (entry == null)
                    return false;

                // Fast filter + full verification for correctness
                if (randomParts[i] == idTag && entry.Header->Id == id)
                {
                    _count--;
                    ShiftBack(i);
                    if (_count < _shrinkThreshold && _capacity > InitialCapacity)
                        Resize(_capacity / 2);
                    return true;
                }

                i = (i + 1) & mask;
            }
#if THREAD_SAFE
            }
  finally
            {
           _lock.Exit();
    }
#endif
        }

        /// <summary>
        /// Fills the empty slot at <paramref name="gapIndex"/> by shifting 
        /// subsequent colliding items backward using backward-shift deletion.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShiftBack(int gapIndex)
        {
            int curr = (gapIndex + 1) & _mask;
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;
            int capacity = _capacity;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            while (true)
            {
                TBody currentEntry = entries[curr];

                if (currentEntry == null)
                {
                    entries[gapIndex] = null;
                    randomParts[gapIndex] = 0;
                    return;
                }

                // Recompute ideal slot from SlotComputation
                int idealSlot = currentEntry.Header->Id.SlotComputation & mask;

                int distToGap = (gapIndex - idealSlot + capacity) & mask;
                int distToCurr = (curr - idealSlot + capacity) & mask;

                if (distToGap < distToCurr)
                {
                    entries[gapIndex] = currentEntry;
                    randomParts[gapIndex] = randomParts[curr];
                    gapIndex = curr;
                }

                curr = (curr + 1) & mask;
            }
        }

        private void Resize(int newCapacity)
        {
            var oldEntries = _entries;
            int oldCapacity = _capacity;

            _capacity = newCapacity;
            _mask = _capacity - 1;
            _entries = new TBody[_capacity];
            _randomParts = new short[_capacity];
            UpdateThresholds();

            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            for (int i = 0; i < oldCapacity; i++)
            {
                TBody e = oldEntries[i];
                if (e != null)
                {
                    short tag = e.Header->Id.RandomPartTag;
                    int index = e.Header->Id.SlotComputation & mask;

                    while (entries[index] != null)
                        index = (index + 1) & mask;

                    entries[index] = e;
                    randomParts[index] = tag;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateThresholds()
        {
            _resizeThreshold = (int)(_capacity * LoadFactor);
            _shrinkThreshold = (int)(_capacity * ShrinkFactor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PowerOf2(int n)
        {
            if (n < 2) return 2;
            n--; n |= n >> 1; n |= n >> 2; n |= n >> 4; n |= n >> 8; n |= n >> 16;
            return n + 1;
        }

        // --- ENUMERATOR SUPPORT ---

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TBody[] GetEntriesArray() => _entries;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
#if THREAD_SAFE
            _lock.Enter();
            try
            {
#endif
                return new Enumerator(_entries);
#if THREAD_SAFE
            } finally
            {
                _lock.Exit();
            }
#endif
        }

        public struct Enumerator : IEnumerator<TBody>
        {
            private readonly TBody[] _entries;
            private int _index;
            private TBody _current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(TBody[] entries)
            {
                _entries = entries;
                _index = -1;
                _current = null;
            }

            public TBody Current {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _current;
            }

            object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                var entries = _entries;
                int length = entries.Length;

                while (++_index < length)
                {
                    TBody entry = entries[_index];
                    if (entry != null)
                    {
                        _current = entry;
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
                _current = null;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Clears all entries from the index.
        /// </summary>
        public void Clear()
        {
#if THREAD_SAFE
            _lock.Enter();
      try
      {
#endif
            Array.Clear(_entries, 0, _capacity);
            Array.Clear(_randomParts, 0, _capacity);
            _count = 0;
#if THREAD_SAFE
            }
  finally
  {
   _lock.Exit();
            }
#endif
        }
    }
}
