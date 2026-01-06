using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a high-performance, tombstone-aware hash map for managing segment references and their associated
/// metadata, supporting efficient versioned lookups, insertions, and removals.
/// </summary>
/// <remarks>SegmentGhostMap is designed for scenarios where segment entries may be frequently updated or removed,
/// and where versioned access is required. The map uses tombstones to mark removed entries, enabling efficient probing
/// and minimizing unnecessary allocations. It supports snapshot isolation for enumeration, allowing safe iteration even
/// if the map is resized concurrently. Thread safety is provided for mutation operations (such as Set and Remove) via
/// internal locking, but callers should be aware that enumerators operate on a snapshot and do not reflect subsequent
/// changes. The map automatically resizes and reclaims tombstones to maintain performance as the number of entries
/// changes.</remarks>
public sealed unsafe class SegmentGhostMap<TSegmentStore>
        where TSegmentStore : ISegmentStore
{
    private const float LoadFactor = 0.75f;
    private const int InitialCapacity = 16;

    private readonly TSegmentStore _store;
    private SegmentReference[] _entries;

    private int _count;             // Number of valid, live items
    private int _occupied;          // Number of used slots (Live + Tombstones)
    private int _tombstoneCount;    // Number of tombstone slots

    private int _capacity;
    private int _mask;
    private int _resizeThreshold;

    private ShortSpinLock _lock;

    public int Count => _count;

    public int Capacity => _capacity;

    public SegmentGhostMap(TSegmentStore store, int initialCapacity = InitialCapacity)
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

            GhostId newId = h->Id;
            long newTxnId = h->TxnId;

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
                // 4. Check for logical duplicate (same Id + TxnId but different reference)
                else
                {
                    var existingHeader = (GhostHeader*)_store.ToGhostHeaderPointer(current);
                    if (existingHeader != null && existingHeader->Id == newId && existingHeader->TxnId == newTxnId)
                    {
                        // Logical duplicate found - update the reference to the new one
                        _entries[index] = r;
                        return;
                    }
                }

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