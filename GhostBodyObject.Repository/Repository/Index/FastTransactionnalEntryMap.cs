using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

public unsafe class FastTransactionnalEntryMap
{
    private const float LoadFactor = 0.75f;
    private const float ShrinkFactor = 0.25f; // Shrink when 25% full
    private const int InitialCapacity = 16;

    private MemorySegmentStore _store;
    private SegmentReference[] _entries;
    private int _count;
    private int _capacity;
    private int _mask;
    private int _resizeThreshold; // When to grow (High Watermark)
    private int _shrinkThreshold; // When to shrink (Low Watermark)

    private ShortSpinLock _lock;

    public int Count => _count;

    public FastTransactionnalEntryMap(MemorySegmentStore store, int initialCapacity = InitialCapacity)
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
    public void Set(ref EntryWithTxnId entry)
    {
#if THREAD_SAFE
        _lock.Enter();
        try {
#endif
        if (_count >= _resizeThreshold)
            Resize(_capacity * 2);

        int index = Hash(ref entry.Id) & _mask;

        while (true)
        {
            ref SegmentReference current = ref _entries[index];

            // 1. Found Empty Slot: Insert new distinct version
            
            if (current.Id == Guid.Empty)
            {
                current = entry;
                _count++;
                return;
            }

            // 2. Found Exact Match (Id + TxnId): Overwrite/Update
            if (current.Id == entry.Id && current.TxnId == entry.TxnId)
            {
                current = entry;
                return;
            }

            // 3. Collision (Same Id/Diff TxnId OR Diff Id): Continue probing
            index = (index + 1) & _mask;
        }
#if THREAD_SAFE
        } finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Finds the entry with the specified Id and the highest TxnId <= maxTxnId.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EntryWithTxnId GetRef(Guid id, long maxTxnId, out bool exists)
    {
#if THREAD_SAFE
        _lock.Enter();
        try {
#endif
        int index = Hash(ref id) & _mask;

        // We need to store the index of the "best" candidate found so far.
        // Pointers or refs cannot be easily stored in a local var while iterating 
        // without pinning issues in unsafe contexts, so we store the index.
        int bestIndex = -1;
        long bestTxnFound = long.MinValue;

        while (true)
        {
            ref EntryWithTxnId current = ref _entries[index];

            // Stop only when we hit a truly empty slot (end of probe chain)
            if (current.Id == Guid.Empty)
            {
                if (bestIndex != -1)
                {
                    exists = true;
                    return ref _entries[bestIndex];
                }
                exists = false;
                return ref current; // Return the empty ref
            }

            // Check for Guid match
            if (current.Id == id)
            {
                // Check logic: Must be <= maxTxnId
                if (current.TxnId <= maxTxnId)
                {
                    // Optimization: Exact match is always the best possible result.
                    if (current.TxnId == maxTxnId)
                    {
                        exists = true;
                        return ref current;
                    }

                    // Otherwise, keep it if it is "better" (higher) than what we have
                    if (current.TxnId > bestTxnFound)
                    {
                        bestTxnFound = current.TxnId;
                        bestIndex = index;
                    }
                }
            }

            index = (index + 1) & _mask;
        }
#if THREAD_SAFE
        } finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Removes a specific version (Id + TxnId).
    /// </summary>
    public bool Remove(Guid id, long txnId)
    {
#if THREAD_SAFE
        _lock.Enter();
        try {
#endif
        int i = Hash(ref id) & _mask;
        while (true)
        {
            ref EntryWithTxnId entry = ref _entries[i];

            if (entry.Id == Guid.Empty) return false;

            // Only remove if BOTH Id and TxnId match
            if (entry.Id == id && entry.TxnId == txnId)
            {
                _count--;
                ShiftBack(i); // Standard ShiftBack works perfectly here

                if (_count < _shrinkThreshold && _capacity > InitialCapacity)
                    Resize(_capacity / 2);

                return true;
            }

            i = (i + 1) & _mask;
        }
#if THREAD_SAFE
        } finally { _lock.Exit(); }
#endif
    }

    /// <summary>
    /// Fills the empty slot at <paramref name="gapIndex"/> by shifting 
    /// subsequent colliding items backward.
    /// </summary>
    private void ShiftBack(int gapIndex)
    {
        int curr = (gapIndex + 1) & _mask;

        while (true)
        {
            ref EntryWithTxnId currentEntry = ref _entries[curr];

            // If we hit an empty slot, the chain is broken, we are done.
            if (currentEntry.Id == Guid.Empty)
            {
                // The gap is now truly empty
                _entries[gapIndex].Id = Guid.Empty;
                //_entries[gapIndex].Store = null; // Clear Ref to avoid GC leak
                return;
            }

            // We found a neighbor. We need to calculate its "ideal" position (original hash)
            // to decide if it belongs to the collision chain that covers 'gapIndex'.
            int idealSlot = Hash(ref currentEntry.Id) & _mask;

            // Determine if the ideal slot is "wrapped around" or linear relative to gap
            // We shift IF the item is technically "out of place" relative to the gap.
            // Logic: "Is gapIndex strictly between idealSlot and curr?"
            // (accounting for wrap-around)

            bool needsShift;
            if (idealSlot <= curr)
            {
                // Standard case: Ideal ... Gap ... Curr
                // Shift if Ideal <= Gap
                needsShift = (idealSlot <= gapIndex) && (gapIndex < curr);
                // Actually, simpler logic for circular buffer:
                // If (ideal <= gap < curr) is FALSE, it means the item wants to be closer to gap.
                // Let's use the inverse:
                // We shift if the gap is NOT between ideal and curr.
                needsShift = !(idealSlot <= gapIndex && gapIndex < curr);
            }
            else
            {
                // Wrap around case: Gap ... Curr ... Ideal  OR  Curr ... Ideal ... Gap
                needsShift = !(idealSlot <= gapIndex || gapIndex < curr);
            }

            // Simplified "cyclical distance" check:
            // distance(ideal, gap) < distance(ideal, curr)
            // If the gap is closer to the ideal start than the current position is, move it!
            int distToGap = (gapIndex - idealSlot + _capacity) & _mask;
            int distToCurr = (curr - idealSlot + _capacity) & _mask;

            if (distToGap < distToCurr)
            {
                // Move current item into the gap
                _entries[gapIndex] = currentEntry;

                // The gap effectively moves to 'curr'
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
        _entries = new EntryWithTxnId[_capacity];
        UpdateThresholds();

        // Re-insert all active items
        for (int i = 0; i < oldCapacity; i++)
        {
            ref EntryWithTxnId e = ref oldEntries[i];
            if (e.Id != Guid.Empty)
            {
                // Simple Set logic inline for speed
                int index = Hash(ref e.Id) & _mask;
                while (_entries[index].Id != Guid.Empty)
                    index = (index + 1) & _mask;
                _entries[index] = e;
            }
        }
    }

    private void UpdateThresholds()
    {
        _resizeThreshold = (int)(_capacity * LoadFactor);
        _shrinkThreshold = (int)(_capacity * ShrinkFactor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Hash(ref Guid id)
    {
        fixed (Guid* ptr = &id)
        {
            ulong* lptr = (ulong*)ptr;
            ulong hash = lptr[0] ^ lptr[1];
            hash ^= hash >> 33;
            hash *= 0xff51afd7ed558ccd;
            hash ^= hash >> 33;
            return (int)hash;
        }
    }

    private static int PowerOf2(int n)
    {
        if (n < 2) return 2;
        n--; n |= n >> 1; n |= n >> 2; n |= n >> 4; n |= n >> 8; n |= n >> 16;
        return n + 1;
    }
}