using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

/*
public unsafe class NormalSegmentGhostTransactionnalMap
{
    private const float LoadFactor = 0.75f;
    private const float ShrinkFactor = 0.25f; // Shrink when 25% full
    private const int InitialCapacity = 16;

    private ISegmentStore _store;
    private SegmentReference[] _entries;
    private int _count;
    private int _capacity;
    private int _mask;
    private int _resizeThreshold; // When to grow (High Watermark)
    private int _shrinkThreshold; // When to shrink (Low Watermark)

    private ShortSpinLock _lock;

    public int Count => _count;

    public NormalSegmentGhostTransactionnalMap(ISegmentStore store, int initialCapacity = InitialCapacity)
    {
        _store = store;
        if (initialCapacity < InitialCapacity) initialCapacity = InitialCapacity;
        _capacity = PowerOf2(initialCapacity);
        _mask = _capacity - 1;
        _entries = new SegmentReference[_capacity];

        UpdateThresholds();
    }

    /// <summary>
    /// Adds a specific version of the entry.
    /// Supports multiple entries with the same Id but different TxnId.
    /// </summary>
    public void Set(SegmentReference r, GhostHeader* h)
    {
#if WRITE_THREAD_SAFE
        _lock.Enter();
        try
        {
#endif
            if (_count >= _resizeThreshold)
                Resize(_capacity * 2);

            int index = h->Id.UpperRandomPart & _mask;

            while (true)
            {
                SegmentReference current = _entries[index];
                if (current.IsEmpty)
                {
                    _entries[index] = r;
                    _count++;
                    return;
                }
                if (current.Value == r.Value)
                    return;
                index = (index + 1) & _mask;
            }
#if WRITE_THREAD_SAFE
        }
        finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Finds the entry with the specified Id and the highest TxnId <= maxTxnId.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(GhostId id, long maxTxnId, out SegmentReference r)
    {
        var entries = _entries;
        int index = id.UpperRandomPart & _mask;
        int bestIndex = -1;
        long bestTxnFound = long.MinValue;
#if READ_THREAD_SAFE
        _lock.Enter();
        try {
#endif

        while (true)
        {
            SegmentReference current = entries[index];
            if (current.IsEmpty)
            {
                if (bestIndex != -1)
                {
                    r = entries[bestIndex];
                    return true;
                }
                r = default;
                return false;
            }

            var h = (GhostHeader*)_store.ToGhostHeaderPointer(current);
            if (h != null && h->Id == id)
            {
                var txnId = h->TxnId;
                if (txnId <= maxTxnId)
                {
                    if (txnId == maxTxnId)
                    {
                        r = current;
                        return true;
                    }
                    if (txnId > bestTxnFound)
                    {
                        bestTxnFound = txnId;
                        bestIndex = index;
                    }
                }
            }

            index = (index + 1) & _mask;
        }
#if READ_THREAD_SAFE
        }
        finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Removes a specific version (Id + TxnId).
    /// </summary>
    public bool Remove(GhostId id, long txnId)
    {
        int index = id.UpperRandomPart & _mask;
#if WRITE_THREAD_SAFE
        _lock.Enter();
        try
        {
#endif
            while (true)
            {
                SegmentReference entry = _entries[index];

                if (entry.IsEmpty)
                    return false;

                var h = (GhostHeader*)_store.ToGhostHeaderPointer(entry);
                if (h != null && h->Id == id && h->TxnId == txnId)
                {
                    _count--;
                    ShiftBack(index, id);
                    if (_count < _shrinkThreshold && _capacity > InitialCapacity)
                        Resize(_capacity / 2);
                    return true;
                }

                index = (index + 1) & _mask;
            }
#if WRITE_THREAD_SAFE
        }
        finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Fills the empty slot at <paramref name="gapIndex"/> by shifting 
    /// subsequent colliding items backward.
    /// </summary>
    private void ShiftBack(int gapIndex, GhostId id)
    {
        int curr = (gapIndex + 1) & _mask;

        while (true)
        {
            SegmentReference currentEntry = _entries[curr];
            if (currentEntry.IsEmpty)
            {
                _entries[gapIndex] = default;
                return;
            }
            int idealSlot = id.UpperRandomPart & _mask;
            bool needsShift;
            if (idealSlot <= curr)
            {
                needsShift = (idealSlot <= gapIndex) && (gapIndex < curr);
                needsShift = !(idealSlot <= gapIndex && gapIndex < curr);
            }
            else
            {
                needsShift = !(idealSlot <= gapIndex || gapIndex < curr);
            }
            int distToGap = (gapIndex - idealSlot + _capacity) & _mask;
            int distToCurr = (curr - idealSlot + _capacity) & _mask;

            if (distToGap < distToCurr)
            {
                _entries[gapIndex] = currentEntry;
                gapIndex = curr;
            }

            curr = (curr + 1) & _mask;
        }
    }

    private void Resize(int newCapacity)
    {
        var oldEntries = _entries;
        int oldCapacity = _capacity;

        _capacity = newCapacity;
        _mask = _capacity - 1;
        _entries = new SegmentReference[_capacity];
        UpdateThresholds();

        // Re-insert all active items
        var count = 0;
        for (int i = 0; i < oldCapacity; i++)
        {
            SegmentReference e = oldEntries[i];
            if (!e.IsEmpty)
            {
                var h = (GhostHeader*)_store.ToGhostHeaderPointer(e);
                if (h != null)
                {
                    int index = h->Id.UpperRandomPart & _mask;
                    while (!_entries[index].IsEmpty)
                        index = (index + 1) & _mask;
                    _entries[index] = e;
                    count++;
                }
            }
        }
        _count = count;
    }

    private void UpdateThresholds()
    {
        _resizeThreshold = (int)(_capacity * LoadFactor);
        _shrinkThreshold = (int)(_capacity * ShrinkFactor);
    }

    private static int PowerOf2(int n)
    {
        if (n < 2) return 2;
        n--; n |= n >> 1; n |= n >> 2; n |= n >> 4; n |= n >> 8; n |= n >> 16;
        return n + 1;
    }
}
*/

public unsafe class SegmentGhostTransactionnalMap
{
    private const float LoadFactor = 0.75f;
    private const int InitialCapacity = 16;

    private readonly ISegmentStore _store;
    private SegmentReference[] _entries;

    private int _count;             // Number of valid, live items
    private int _occupied;          // Number of used slots (Live + Tombstones)
    private int _tombstoneCount;    // Number of tombstone slots

    private int _capacity;
    private int _mask;
    private int _resizeThreshold;

    private ShortSpinLock _lock;

    public int Count => _count;

    public SegmentGhostTransactionnalMap(ISegmentStore store, int initialCapacity = InitialCapacity)
    {
        _store = store;
        if (initialCapacity < InitialCapacity) initialCapacity = InitialCapacity;
        _capacity = PowerOf2(initialCapacity);
        _mask = _capacity - 1;
        _entries = new SegmentReference[_capacity];
        UpdateThresholds();
    }

    /// <summary>
    /// Adds or updates an entry using Tombstone-aware probing.
    /// </summary>
    public void Set(SegmentReference r, GhostHeader* h)
    {
        _lock.Enter();
        try
        {
            // Check if we need to resize based on OCCUPIED slots (Live + Graves)
            if (_occupied >= _resizeThreshold)
            {
                int newCapacity;

                // Case 1: Very few live items? Shrink.
                // Example: Capacity 1000, Count 100. (10% full).
                // We shrink to 500.
                if (_count < _capacity / 4)
                {
                    newCapacity = _capacity / 2;
                }
                // Case 2: Lots of live items? Grow.
                // Example: Capacity 1000, Count 600 (60% full).
                // We grow to 2000.
                else if (_count > _capacity / 2)
                {
                    newCapacity = _capacity * 2;
                }
                // Case 3: Moderate live items (25% - 50%). Cleanup only.
                // Example: Capacity 1000, Count 400 (40% full).
                // The array is clogged with tombstones, but we have enough data 
                // to justify the current size. Just clean the graves.
                else
                {
                    newCapacity = _capacity;
                }

                // Safety clamp
                if (newCapacity < InitialCapacity) newCapacity = InitialCapacity;

                Resize(newCapacity);
            }

            int index = h->Id.UpperRandomPart & _mask;
            int firstTombstoneIndex = -1; // Optimization: Insert into first grave found

            while (true)
            {
                // Read value directly (atomic 64-bit read)
                SegmentReference current = _entries[index];

                // 1. Found Empty Slot?
                if (current.IsEmpty())
                {
                    // If we passed a tombstone earlier, recycle it!
                    if (firstTombstoneIndex != -1)
                    {
                        _entries[firstTombstoneIndex] = r;
                        _count++;
                        _tombstoneCount--;
                        // _occupied does not change (Tombstone -> Live)
                        return;
                    }

                    // No tombstone found, insert here.
                    _entries[index] = r;
                    _count++;
                    _occupied++;
                    return;
                }

                // 2. Found Tombstone?
                if (current.IsTombstone())
                {
                    // Remember this spot if it's the first one we see
                    if (firstTombstoneIndex == -1) firstTombstoneIndex = index;
                }
                // 3. Found Duplicate? (Same exact reference)
                else if (current.Value == r.Value)
                {
                    return;
                }
                // Note: Logic for colliding IDs (different TxnId) is handled by linear probing
                // We just continue to the next slot.

                index = (index + 1) & _mask;
            }
        }
        finally { _lock.Exit(); }
    }

    /// <summary>
    /// Finds the entry. Tombstones are treated as "keep looking".
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(GhostId id, long maxTxnId, out SegmentReference r)
    {
    _redo:
        // 1. Capture Reference
        var entries = _entries;

        // 2. Derive Mask Locally (Fixes Crash Bug)
        // entries.Length is extremely fast (header read), safer than reading a volatile _mask
        int mask = entries.Length - 1;

        int index = id.UpperRandomPart & mask;
        int bestIndex = -1;
        long bestTxnFound = long.MinValue;

        // Optional: Safety counter to prevent infinite loops if memory is corrupted
        // uint attempts = 0; 

        while (true)
        {
            // if (attempts++ > mask) goto _redo; // Optional safety guard

            SegmentReference current = entries[index];

            // --- STOP AT EMPTY ---
            if (current.IsEmpty())
            {
                // CRITICAL: We hit end of chain. 
                // BEFORE returning any result (Found or Not Found), we must ensure 
                // the map didn't grow while we were looking at this specific array.
                if (entries != _entries)
                    goto _redo;

                // If we are here, the array is stable. The result is valid.
                if (bestIndex != -1)
                {
                    r = entries[bestIndex];
                    return true;
                }

                r = default;
                return false;
            }

            // --- SKIP TOMBSTONES ---
            if (!current.IsTombstone())
            {
                // --- VALID ITEM CHECK ---
                var h = (GhostHeader*)_store.ToGhostHeaderPointer(current);

                // Pointer check + ID match
                if (h != null && h->Id == id)
                {
                    long txnId = h->TxnId;
                    if (txnId <= maxTxnId)
                    {
                        // Exact Match Optimization
                        if (txnId == maxTxnId)
                        {
                            r = current;
                            // Technically, we found the *maximum possible* version requested.
                            // Even if a resize happened, we found exactly what was asked.
                            // However, strictly checking for resize ensures pointer validity.
                            if (entries != _entries)
                                goto _redo;

                            return true;
                        }

                        // Keep track of best candidate
                        if (txnId > bestTxnFound)
                        {
                            bestTxnFound = txnId;
                            bestIndex = index;
                        }
                    }
                }
            }

            index = (index + 1) & mask;
        }
    }

    /// <summary>
    /// Removes an entry by marking it as a Tombstone.
    /// </summary>
    public bool Remove(GhostId id, long txnId)
    {
        int index = id.UpperRandomPart & _mask;

        _lock.Enter();
        try
        {
            while (true)
            {
                SegmentReference current = _entries[index];

                if (current.IsEmpty())
                    return false;

                if (!current.IsTombstone())
                {
                    var h = (GhostHeader*)_store.ToGhostHeaderPointer(current);
                    if (h != null && h->Id == id && h->TxnId == txnId)
                    {
                        // Mark as Tombstone
                        _entries[index] = SegmentReference.Tombstone;
                        _count--;
                        _tombstoneCount++;

                        // STRATEGY: "Garbage Collection"
                        // If 3/4 of our array is garbage, we force a cleanup.
                        if (_tombstoneCount > (_capacity >> 1) + (_capacity >> 2)) // > 75%
                        {
                            // Optimization: If real data is tiny, shrink. 
                            // Else, keep size (rehash in place).
                            int newCapacity = _capacity;
                            if (_count < (_capacity >> 2)) // Live count < 25%
                                newCapacity = _capacity >> 1; // Shrink by half

                            Resize(Math.Max(newCapacity, InitialCapacity));
                        }

                        return true;
                    }
                }
                index = (index + 1) & _mask;
            }
        }
        finally { _lock.Exit(); }
    }

    private void Resize(int newCapacity)
    {
        // 1. Create new array
        var newEntries = new SegmentReference[newCapacity];
        int newMask = newCapacity - 1;
        var oldEntries = _entries;

        // 2. Rehash
        for (int i = 0; i < oldEntries.Length; i++)
        {
            SegmentReference e = oldEntries[i];
            if (e.IsValid()) // Filters Empty AND Tombstones
            {
                var h = (GhostHeader*)_store.ToGhostHeaderPointer(e);
                if (h != null)
                {
                    int index = h->Id.UpperRandomPart & newMask;
                    while (!newEntries[index].IsEmpty())
                    {
                        index = (index + 1) & newMask;
                    }
                    newEntries[index] = e;
                }
            }
        }

        // 3. Update State
        _count = _count; // Count remains same (we filtered tombstones)
        _tombstoneCount = 0;
        _occupied = _count; // Occupied now equals count (no tombstones)
        _capacity = newCapacity;
        _mask = newMask;
        UpdateThresholds();

        // 4. Publish (Atomic Swap)
        // Order matters: Update generation, then swap array.
        // Actually, for the reader logic:
        // If we swap array first, reader sees new array but old Gen -> mismatch on next read? No.
        // We need to increment Gen to invalidate current readers.
        _entries = newEntries; // Volatile write
    }

    private void UpdateThresholds()
    {
        _resizeThreshold = (int)(_capacity * LoadFactor);
    }

    private static int PowerOf2(int n)
    {
        if (n < 2) return 2;
        n--; n |= n >> 1; n |= n >> 2; n |= n >> 4; n |= n >> 8; n |= n >> 16;
        return n + 1;
    }

    // -----------------------------------------------------------------
    // LIGHTNING FAST ENUMERATOR (No Copy, No Allocation)
    // -----------------------------------------------------------------

    public Enumerator GetEnumerator() => new Enumerator(_entries);

    public struct Enumerator
    {
        private readonly SegmentReference[] _entries;
        private int _index;
        private SegmentReference _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(SegmentReference[] entries)
        {
            // We hold a reference to the array.
            // If Resize() happens, we keep iterating the OLD array (Snapshot Isolation).
            _entries = entries;
            _index = 0;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // Hoisting length to local var for speed
            var arr = _entries;
            int len = arr.Length;
            int i = _index;

            while (i < len)
            {
                var val = arr[i];
                i++;

                // The magic: Check IsValid (Not 0 AND Not MaxValue)
                // This filters out both Empty slots and Tombstones
                if (val.IsValid())
                {
                    _current = val;
                    _index = i;
                    return true;
                }
            }

            _index = i;
            return false;
        }

        public SegmentReference Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    /// <summary>
    /// Returns an enumerator that yields ONLY the latest version of each key visible at maxTxnId.
    /// Logic: For every candidate found, it probes the map to ensure no newer version exists.
    /// </summary>
    public DeduplicatedEnumerator GetDeduplicatedEnumerator(long maxTxnId)
        => new DeduplicatedEnumerator(_entries, _store, maxTxnId);

    public struct DeduplicatedEnumerator
    {
        private readonly SegmentReference[] _entries;
        private readonly ISegmentStore _store;
        private readonly long _maxTxnId;
        private readonly int _mask;

        private int _index;
        private SegmentReference _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DeduplicatedEnumerator(SegmentReference[] entries, ISegmentStore store, long maxTxnId)
        {
            // Snapshot isolation: We work on the array captured at creation
            _entries = entries;
            _store = store;
            _maxTxnId = maxTxnId;
            _mask = entries.Length - 1; // Capture mask matching the array
            _index = 0;
            _current = default;
        }

        public bool MoveNext()
        {
            var entries = _entries;
            var store = _store;
            long maxTxn = _maxTxnId;
            int mask = _mask;
            int len = entries.Length;

            // Resume linear scan
            while (_index < len)
            {
                SegmentReference candidate = entries[_index];
                _index++; // Advance index for next call

                if (!candidate.IsValid()) continue; // Skip Empty & Tombstone

                // 1. Check Visibility
                GhostHeader* h = (GhostHeader*)store.ToGhostHeaderPointer(candidate);
                if (h == null || h->TxnId > maxTxn) continue;

                // 2. The "Winner Check" (Deduplication)
                // We pause the linear scan to ask: "Is this candidate the BEST version of this Key?"
                // We perform a probe on the *same snapshot* to find the authoritative answer.
                if (IsBestVersion(h->Id, candidate, entries, mask, maxTxn, store))
                {
                    _current = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Probes the map to find the best version of 'id'. 
        /// Returns true ONLY if 'candidate' is that best version.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBestVersion(
            GhostId id,
            SegmentReference candidate,
            SegmentReference[] entries,
            int mask,
            long maxTxnId,
            ISegmentStore store)
        {
            int index = id.UpperRandomPart & mask;

            // We need to find the "Best" reference in the chain
            SegmentReference bestRef = default;
            long bestTxnFound = long.MinValue;

            while (true)
            {
                SegmentReference current = entries[index];

                // Stop at Empty (End of Chain)
                if (current.IsEmpty())
                    break;

                if (!current.IsTombstone())
                {
                    var h = (GhostHeader*)store.ToGhostHeaderPointer(current);
                    if (h != null && h->Id == id)
                    {
                        long txnId = h->TxnId;
                        if (txnId <= maxTxnId)
                        {
                            // Optimization: If we find a version strictly newer than our candidate, 
                            // we verify immediately that our candidate is NOT the winner.
                            // However, we must continue to handle exact duplicates (if any exist) strictly.

                            // Track the global winner in this chain
                            if (txnId > bestTxnFound)
                            {
                                bestTxnFound = txnId;
                                bestRef = current;
                            }
                        }
                    }
                }

                index = (index + 1) & mask;
            }

            // Verification: 
            // We only yield the candidate if it matches the Best Reference found.
            // Note: We compare Values (pointers/indices) to ensure strict identity.
            return bestRef.Value == candidate.Value;
        }

        public SegmentReference Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}