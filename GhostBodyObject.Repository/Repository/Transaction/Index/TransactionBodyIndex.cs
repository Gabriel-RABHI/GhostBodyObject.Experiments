using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Transaction.Index
{
    /// <summary>
    /// High-performance hash index for transaction body objects.
    /// Uses open addressing with linear probing and stores cached random parts
    /// to minimize cache misses during lookups.
    /// </summary>
    public unsafe class TransactionBodyIndex<TBody>
        where TBody : BodyBase
    {
        // Parallel arrays for better cache locality during probing
        // Using uint (4 bytes) instead of ulong (8 bytes) for 2x cache density
        // Correctness is guaranteed by full GhostId verification on match
        private uint[] _randomParts;  // Cached lower 32 bits of GhostId.RandomPart
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
        private ShortMonitor _lock;
#endif

        public int Count => _count;

        public int Capacity => _capacity;

        public TransactionBodyIndex(int initialCapacity = InitialCapacity)
        {
            if (initialCapacity < InitialCapacity) initialCapacity = InitialCapacity;
            _capacity = PowerOf2(initialCapacity);
            _mask = _capacity - 1;
            _entries = new TBody[_capacity];
            _randomParts = new uint[_capacity];

            UpdateThresholds();
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
            uint entryRandom = (uint)entryId.LowerRandomPart;
            int index = (int)entryRandom & _mask;
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            while (true)
            {
                if (entries[index] == null)
                {
                    entries[index] = entry;
                    randomParts[index] = entryRandom;
                    _count++;
                    return;
                }

                // Fast filter: compare cached random parts first (32-bit, excellent cache density)
                // Then verify full ID match to handle RandomPart collisions (~1 in 2^32)
                if (randomParts[index] == entryRandom && entries[index].Header->Id == entryId)
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
            uint idRandom = (uint)id.LowerRandomPart;
            int index = (int)idRandom & _mask;
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            while (true)
            {
                TBody current = entries[index];
                if (current == null)
                {
                    exists = false;
                    return null;
                }
                // Fast filter + full verification for correctness
                if (randomParts[index] == idRandom && current.Header->Id == id)
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
            uint idRandom = (uint)id.LowerRandomPart;
            int i = (int)idRandom & _mask;
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            while (true)
            {
                TBody entry = entries[i];
                if (entry == null)
                    return false;

                // Fast filter + full verification for correctness
                if (randomParts[i] == idRandom && entry.Header->Id == id)
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

            while (true)
            {
                TBody currentEntry = entries[curr];

                if (currentEntry == null)
                {
                    entries[gapIndex] = null;
                    randomParts[gapIndex] = 0;
                    return;
                }

                // Use cached random part for ideal slot calculation
                int idealSlot = (int)randomParts[curr] & mask;
                
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
            var oldRandomParts = _randomParts;
            int oldCapacity = _capacity;

            _capacity = newCapacity;
            _mask = _capacity - 1;
            _entries = new TBody[_capacity];
            _randomParts = new uint[_capacity];
            UpdateThresholds();

            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            for (int i = 0; i < oldCapacity; i++)
            {
                TBody e = oldEntries[i];
                if (e != null)
                {
                    uint random = oldRandomParts[i];
                    int index = (int)random & mask;
                    while (entries[index] != null)
                        index = (index + 1) & mask;
                    entries[index] = e;
                    randomParts[index] = random;
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
        public Enumerator GetEnumerator() => new Enumerator(_entries);

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

            public TBody Current
            {
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
