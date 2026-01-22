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
#undef NATIVE_LOCK

using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.CompilerServices;

/// <summary>
/// Represents a high-performance, tombstone-aware hash map for managing segment references and their associated
/// metadata, supporting efficient versioned lookups, insertions, and removals.
/// </summary>
/// <remarks>
/// <para>
/// SegmentGhostMap is designed for scenarios where segment entries may be frequently updated or removed,
/// and where versioned access is required. The map uses tombstones to mark removed entries, enabling efficient probing
/// and minimizing unnecessary allocations.
/// </para>
/// <para>
/// <b>Performance Optimization:</b> This implementation uses a parallel <c>RandomParts</c> array that caches
/// the upper 16 bits of each GhostId's UpperRandomPart as a <c>short</c>. During probing, the cached random part 
/// is compared first (fast, L1-cache friendly) before dereferencing the segment pointer. Using <c>short</c>
/// instead of <c>int</c> provides 2x memory savings and 2x cache density (32 entries per cache line vs 16),
/// at the cost of a slightly higher false positive rate (~1 in 65,536 vs ~1 in 4 billion), which is negligible.
/// This reduces cache misses by ~50% in typical workloads, at the cost of 2 additional bytes per entry 
/// (10 bytes total vs 8 bytes).
/// </para>
/// <para>
/// <b>Bit Distribution:</b> When used with <see cref="ShardedSegmentGhostMap{TSegmentStore}"/>, sharding uses
/// the lower bits of LowerRandomPart, slot calculation uses the lower bits of UpperRandomPart, and the fast
/// filter cache uses the upper 16 bits of UpperRandomPart. This ensures all three mechanisms use independent
/// bits for optimal distribution.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Lock-free readers (Get, enumerators) capture a <see cref="MapState"/> snapshot
/// for atomic access to both arrays. Mutators (Set, Remove) use direct field access under lock for 
/// lower overhead, and publish a new MapState atomically at the end of Resize.
/// </para>
/// <para>
/// It supports snapshot isolation for enumeration, allowing safe iteration even if the map is resized concurrently.
/// Thread safety is provided for mutation operations (such as Set and Remove) via internal locking, but callers 
/// should be aware that enumerators operate on a snapshot and do not reflect subsequent changes.
/// </para>
/// </remarks>
public sealed unsafe class SegmentGhostMap<TSegmentStore>
   where TSegmentStore : ISegmentStore
{
    private const float LoadFactor = 0.75f;
    private const int InitialCapacity = 16;

    // Sentinel value for tombstone in RandomParts array
    private const short RandomPart_Tombstone = short.MinValue; // 0x8000

    /// <summary>
    /// Holds both parallel arrays together to enable atomic snapshot capture for lock-free readers.
    /// This prevents race conditions where a reader might see entries from one resize cycle 
    /// and randomParts from another.
    /// </summary>
    internal sealed class MapState
    {
        public readonly SegmentReference[] Entries;
        public readonly short[] RandomParts;
        public readonly int Mask;

        public MapState(SegmentReference[] entries, short[] randomParts, int mask)
        {
            Entries = entries;
            RandomParts = randomParts;
            Mask = mask;
        }
    }

    private readonly TSegmentStore _store;

    // Direct field access for mutators (under lock) - lower overhead
    private SegmentReference[] _entries;
    private short[] _randomParts;
    private int _mask;

    // Atomic snapshot for lock-free readers - published at end of Resize
    private MapState _state;

    private int _count; // Number of valid, live items
    private int _occupied;        // Number of used slots (Live + Tombstones)
    private int _tombstoneCount;    // Number of tombstone slots

    private int _capacity;
    private int _resizeThreshold;

#if NATIVE_LOCK
#else
    private ShortSpinLock _lock;
#endif

    public int Count => _count;

    public int Capacity => _capacity;

    public SegmentGhostMap(TSegmentStore store, int initialCapacity = InitialCapacity)
    {
        _store = store;
        if (initialCapacity < InitialCapacity) initialCapacity = InitialCapacity;
        _capacity = PowerOf2(initialCapacity);
        _mask = _capacity - 1;
        _entries = new SegmentReference[_capacity];
        _randomParts = new short[_capacity];
        _state = new MapState(_entries, _randomParts, _mask);
        UpdateThresholds();
    }

    /// <summary>
    /// Adds or Updates the entry using Tombstone-aware probing.
    /// Uses direct field access for lower overhead (protected by lock).
    /// Removes versions superseded by newer ones (relative to bottomTxnId).
    /// 
    /// It is usefull for :
    /// - initial map building : we set the bottomTxnId to the current txnId.
    /// - transaction commit : we set the bottomTxnId to the known min opened transaction.
    /// 
    /// If bottomTxnId == h.TxnId, it means we are inserting the first version of this ghost,
    /// we have to replace any older version.
    /// 
    /// If bottomTxnId < h.TxnId, when we find an older version, we have to remove it.
    /// 
    /// 
    ///   5
    ///   |
    ///   10
    ///   |
    ///   18
    ///   |
    ///   26
    ///   |
    ///   32
    /// 
    /// We insert version 35 with bottomTxnId = 20
    /// We must preserve 18, 26, 32. 5 and 10 must be removed.
    /// But when enumerate, we don't know if 5 is not the latest version vor 20.
    /// Only when we find 10, we know that 5 is not valid anymore.
    /// We replace 5.
    /// 
    ///   35
    ///   |
    ///   10
    ///   |
    ///   18
    ///   |
    ///   26
    ///   |
    ///   32
    ///   
    /// We continue tu enumerate while Txn < bottomTxnId
    /// 18 is the latest valid version < 20
    /// We remove 10 :
    /// 
    ///   35
    ///   |
    ///   x
    ///   |
    ///   18
    ///   |
    ///   26
    ///   |
    ///   32
    ///   
    /// We continue : 26 is over 20, we stop.
    /// 
    /// -------- Next update
    /// 
    /// We insert 40, bottomTxnId = 30
    /// 35 is above. We found empty slot. We insert.
    /// 
    ///   35
    ///   |
    ///   40
    ///   |
    ///   18
    ///   |
    ///   26
    ///   |
    ///   32
    ///   
    /// We insert 45, bottomTxnId = 33
    /// 
    ///   35
    ///   |
    ///   40
    ///   |
    ///   18
    ///   |
    ///   26
    ///   |
    ///   32
    ///   
    /// 35 is above 33. 40 is above. We find 18 - it is potentiolly the 33 official.
    /// 26 is above 18, we replace 18.
    /// 
    ///   35
    ///   |
    ///   40
    ///   |
    ///   45
    ///   |
    ///   26
    ///   |
    ///   32
    /// 
    /// We find an empty slot, we stop.
    /// 
    /// </summary>
    public void SetAndRemove(SegmentReference r, GhostHeader* h, long bottomTxnId)
    {
#if NATIVE_LOCK
        lock (this)
#else
        _lock.Enter();
        try
#endif
        {
            // Check if we need to resize based on OCCUPIED slots (Live + Graves)
            if (_occupied >= _resizeThreshold)
            {
                int newCapacity;

                // Case 1: Very few live items? Shrink.
                if (_count < _capacity / 4)
                {
                    newCapacity = _capacity / 2;
                }
                // Case 2: Lots of live items? Grow.
                else if (_count > _capacity / 2)
                {
                    newCapacity = _capacity * 2;
                }
                // Case 3: Moderate live items (25% - 50%). Cleanup only.
                else
                {
                    newCapacity = _capacity;
                }

                // Safety clamp
                if (newCapacity < InitialCapacity)
                    newCapacity = InitialCapacity;

                Resize(newCapacity);
            }

            // Direct field access - we're under lock
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            GhostId newId = h->Id;
            short newRandomPart = newId.RandomPartTag;
            long newTxnId = h->TxnId;

            // Slot calculation uses specific bit-range
            int index = newId.SlotComputation & mask;

            // Track the "best" version found so far that is <= bottomTxnId.
            // Any other version found <= bottomTxnId is strictly inferior and can be removed/recycled.
            int bestOldSlotIndex = -1;
            long bestOldTxnId = -1;

            bool insertionPending = true;
            int firstTombstoneIndex = -1;

            while (true)
            {
                SegmentReference current = entries[index];
                short currentRandomPart = randomParts[index];

                // 1. Found Empty Slot?
                if (current.IsEmpty())
                {
                    // End of chain.
                    if (insertionPending)
                    {
                        if (firstTombstoneIndex != -1)
                        {
                            entries[firstTombstoneIndex] = r;
                            randomParts[firstTombstoneIndex] = newRandomPart;
                            _count++;
                            _tombstoneCount--;
                            _store.IncrementSegmentHolderUsage(r);
                        } else
                        {
                            entries[index] = r;
                            randomParts[index] = newRandomPart;
                            _count++;
                            _occupied++;
                            _store.IncrementSegmentHolderUsage(r);
                        }
                    }
                    return;
                }

                // 2. Found Tombstone?
                if (current.IsTombstone())
                {
                    if (firstTombstoneIndex == -1) firstTombstoneIndex = index;
                }
                // 3. Exact duplicate reference?
                else if (current.Value == r.Value)
                {
                    insertionPending = false;
                }
                // 4. GhostId Match?
                else if (currentRandomPart == newRandomPart)
                {
                    var existingHeader = _store.ToGhostHeaderPointer(current);
                    if (existingHeader != null && existingHeader->Id == newId)
                    {
                        long currentTxnId = existingHeader->TxnId;

                        if (currentTxnId == newTxnId)
                        {
                            // Logical duplicate (same ID, same Version) -> Update logic.
                            _store.DecrementSegmentHolderUsage(current);
                            entries[index] = r;
                            _store.IncrementSegmentHolderUsage(r);
                            insertionPending = false;
                        }

                        if (currentTxnId <= bottomTxnId)
                        {
                            // This is a candidate for the "single allowed old version".

                            if (bestOldSlotIndex == -1)
                            {
                                // First candidate found.
                                bestOldSlotIndex = index;
                                bestOldTxnId = currentTxnId;
                            } else
                            {
                                // Compare with current best.
                                int garbageIndex;
                                if (currentTxnId > bestOldTxnId)
                                {
                                    // Current is better. Previous best is garbage.
                                    garbageIndex = bestOldSlotIndex;

                                    // Update best to current
                                    bestOldSlotIndex = index;
                                    bestOldTxnId = currentTxnId;
                                } else
                                {
                                    // Previous best is better (or equal). Current is garbage.
                                    garbageIndex = index;
                                }

                                // Dispose of the garbage slot
                                var garbageRef = entries[garbageIndex];
                                if (insertionPending)
                                {
                                    // Recycle for the new insertion
                                    _store.DecrementSegmentHolderUsage(garbageRef);
                                    entries[garbageIndex] = r;
                                    randomParts[garbageIndex] = newRandomPart;
                                    _store.IncrementSegmentHolderUsage(r);
                                    insertionPending = false;
                                    // Count unchanged (1 replace 1)
                                } else
                                {
                                    // Mark as tombstone
                                    _store.DecrementSegmentHolderUsage(garbageRef);
                                    entries[garbageIndex] = SegmentReference.Tombstone;
                                    randomParts[garbageIndex] = RandomPart_Tombstone;
                                    _count--;
                                    _tombstoneCount++;
                                }
                            }
                        }
                    }
                }

                index = (index + 1) & mask;
            }
        }
#if !NATIVE_LOCK
        finally
        {
            _lock.Exit();
        }
#endif
    }

    /// <summary>
    /// Adds or Updates the entry using Tombstone-aware probing.
    /// Uses direct field access for lower overhead (protected by lock).
    /// </summary>
    public void Set(SegmentReference r, GhostHeader* h)
    {
#if NATIVE_LOCK
        lock (this)
#else
        _lock.Enter();
        try
#endif
        {
            // Check if we need to resize based on OCCUPIED slots (Live + Graves)
            if (_occupied >= _resizeThreshold)
            {
                int newCapacity;

                // Case 1: Very few live items? Shrink.
                if (_count < _capacity / 4)
                {
                    newCapacity = _capacity / 2;
                }
                // Case 2: Lots of live items? Grow.
                else if (_count > _capacity / 2)
                {
                    newCapacity = _capacity * 2;
                }
                // Case 3: Moderate live items (25% - 50%). Cleanup only.
                else
                {
                    newCapacity = _capacity;
                }

                // Safety clamp
                if (newCapacity < InitialCapacity) newCapacity = InitialCapacity;

                Resize(newCapacity);
            }

            // Direct field access - we're under lock
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            GhostId newId = h->Id;
            short newRandomPart = newId.RandomPartTag;
            long newTxnId = h->TxnId;

            // Slot calculation uses specific bit-range
            int index = newId.SlotComputation & mask;
            int firstTombstoneIndex = -1;

            while (true)
            {
                SegmentReference current = entries[index];
                short currentRandomPart = randomParts[index];

                // 1. Found Empty Slot?
                if (current.IsEmpty())
                {
                    // If we passed a tombstone earlier, recycle it!
                    if (firstTombstoneIndex != -1)
                    {
                        entries[firstTombstoneIndex] = r;
                        randomParts[firstTombstoneIndex] = newRandomPart;
                        _count++;
                        _tombstoneCount--;
                        _store.IncrementSegmentHolderUsage(r);
                        return;
                    }

                    // No tombstone found, insert here.
                    entries[index] = r;
                    randomParts[index] = newRandomPart;
                    _count++;
                    _occupied++;
                    _store.IncrementSegmentHolderUsage(r);
                    return;
                }

                // 2. Found Tombstone?
                if (current.IsTombstone())
                {
                    if (firstTombstoneIndex == -1) firstTombstoneIndex = index;
                }
                // 3. Found Duplicate? (Same exact reference)
                else if (current.Value == r.Value)
                {
                    return;
                }
                // 4. Check for logical duplicate (same Id + TxnId but different reference)
                // Fast filter: compare cached random part first
                else if (currentRandomPart == newRandomPart)
                {
                    var existingHeader = _store.ToGhostHeaderPointer(current);
                    if (existingHeader != null && existingHeader->Id == newId && existingHeader->TxnId == newTxnId)
                    {
                        // Logical duplicate found - update the reference
                        _store.DecrementSegmentHolderUsage(current);
                        entries[index] = r;
                        _store.IncrementSegmentHolderUsage(r);
                        // randomParts[index] unchanged (same Id)
                        return;
                    }
                }

                index = (index + 1) & mask;
            }
        }
#if !NATIVE_LOCK
        finally
        {
            _lock.Exit();
        }
#endif
    }

    /// <summary>
    /// Finds the entry. Tombstones are treated as "keep looking".
    /// Uses MapState snapshot for lock-free thread safety.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(GhostId id, long maxTxnId, out SegmentReference r)
    {
    _redo:
        // Capture atomic snapshot for lock-free read
        var state = _state;
        var entries = state.Entries;
        var randomParts = state.RandomParts;
        int mask = state.Mask;

        if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
            throw new InvalidOperationException();

        short searchRandomPart = id.RandomPartTag;
        int index = id.SlotComputation & mask;

        bool foundBest = false;
        SegmentReference bestRef = default;
        long bestTxnFound = long.MinValue;

        while (true)
        {
            SegmentReference current = entries[index];

            // --- STOP AT EMPTY ---
            if (current.IsEmpty())
            {
                // Validate snapshot before returning
                if (state != _state)
                    goto _redo;

                if (foundBest)
                {
                    r = bestRef;
                    return true;
                }

                r = default;
                return false;
            }

            // --- SKIP TOMBSTONES ---
            if (!current.IsTombstone())
            {
                // Fast filter: compare cached random part first (2 bytes, excellent cache density)
                short cachedRandomPart = randomParts[index];
                if (cachedRandomPart == searchRandomPart)
                {
                    // Random parts match - now do the expensive pointer dereference
                    var h = _store.ToGhostHeaderPointer(current);

                    // Full ID verification to handle collisions (~1 in 65,536)
                    if (h != null && h->Id == id)
                    {
                        long txnId = h->TxnId;
                        if (txnId <= maxTxnId)
                        {
                            // Exact Match Optimization
                            if (txnId == maxTxnId)
                            {
                                r = current;
                                if (state != _state)
                                    goto _redo;
                                return true;
                            }

                            // Keep track of best candidate
                            if (txnId > bestTxnFound)
                            {
                                bestTxnFound = txnId;
                                bestRef = current;
                                foundBest = true;
                            }
                        }
                    }
                }
            }

            index = (index + 1) & mask;
        }
    }

    /// <summary>
    /// Removes an entry by marking it as a Tombstone.
    /// Uses direct field access for lower overhead (protected by lock).
    /// </summary>
    public bool Remove(GhostId id, long txnId)
    {
#if NATIVE_LOCK
        lock (this)
#else
        _lock.Enter();
        try
#endif
        {
            // Direct field access - we're under lock
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;

            if (mask >= entries.Length || mask >= randomParts.Length || entries.Length != randomParts.Length)
                throw new InvalidOperationException();

            short searchRandomPart = id.RandomPartTag;
            int index = id.SlotComputation & mask;

            while (true)
            {
                SegmentReference current = entries[index];

                if (current.IsEmpty())
                    return false;

                if (!current.IsTombstone())
                {
                    // Fast filter: compare cached random part first
                    if (randomParts[index] == searchRandomPart)
                    {
                        var h = _store.ToGhostHeaderPointer(current);
                        if (h != null && h->Id == id && h->TxnId == txnId)
                        {
                            // Mark as Tombstone
                            _store.DecrementSegmentHolderUsage(current);
                            entries[index] = SegmentReference.Tombstone;
                            randomParts[index] = RandomPart_Tombstone;
                            _count--;
                            _tombstoneCount++;

                            // Garbage Collection trigger
                            if (_tombstoneCount > (_capacity >> 1) + (_capacity >> 2))
                            {
                                int newCapacity = _capacity;
                                if (_count < (_capacity >> 2))
                                    newCapacity = _capacity >> 1;

                                Resize(Math.Max(newCapacity, InitialCapacity));
                            }

                            return true;
                        }
                    }
                }
                index = (index + 1) & mask;
            }
        }
#if !NATIVE_LOCK
        finally
        {
            _lock.Exit();
        }
#endif
    }

    /// <summary>
    /// Removes all entries that are obsolete regarding the specified bottomTxnId.
    /// Rebuilds the map (like Resize) to filter out garbage versions.
    /// </summary>
    /// <param name="bottomTxnId">The new oldest active transaction ID.</param>
    /// <returns>The number of entries removed.</returns>
    public int Prune(long bottomTxnId)
    {
#if NATIVE_LOCK
        lock (this)
#else
        _lock.Enter();
        try
#endif
        {
            var entries = _entries;
            var randomParts = _randomParts;
            int mask = _mask;
            int removedCount = 0;

            for (int i = 0; i < entries.Length; i++)
            {
                SegmentReference current = entries[i];
                if (current.IsValid())
                {
                    var h = _store.ToGhostHeaderPointer(current);
                    if (h != null)
                    {
                        // Check if candidate for removal (Old Version)
                        if (h->TxnId <= bottomTxnId)
                        {
                            // It is a candidate. Check if superseded by a better old version.
                            if (IsSuperseded(h->Id, h->TxnId, bottomTxnId, entries, randomParts, mask))
                            {
                                // Mark as Tombstone
                                _store.DecrementSegmentHolderUsage(current);
                                entries[i] = SegmentReference.Tombstone;
                                randomParts[i] = RandomPart_Tombstone;
                                _count--;
                                _tombstoneCount++;
                                removedCount++;
                            }
                        }
                    }
                }
            }

            return removedCount;
        }
#if !NATIVE_LOCK
        finally
        {
            _lock.Exit();
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSuperseded(GhostId id, long myTxnId, long bottomTxnId, SegmentReference[] entries, short[] randomParts, int mask)
    {
        short searchRandomPart = id.RandomPartTag;
        int index = id.SlotComputation & mask;

        while (true)
        {
            SegmentReference current = entries[index];
            if (current.IsEmpty()) return false;

            if (!current.IsTombstone())
            {
                if (randomParts[index] == searchRandomPart)
                {
                    var h = _store.ToGhostHeaderPointer(current);
                    if (h != null && h->Id == id)
                    {
                        long thatTxnId = h->TxnId;
                        // Found a version that is ALSO old (<= bottom) but strictly NEWER than me.
                        // So 'I' am superseded.
                        if (thatTxnId <= bottomTxnId && thatTxnId > myTxnId)
                        {
                            return true;
                        }
                    }
                }
            }
            index = (index + 1) & mask;
        }
    }

    /// <summary>
    /// Resizes the map. Uses direct field access and publishes new MapState atomically at the end.
    /// </summary>
    private void Resize(int newCapacity)
    {
        // 1. Create new arrays
        var newEntries = new SegmentReference[newCapacity];
        var newRandomParts = new short[newCapacity];
        int newMask = newCapacity - 1;

        // Direct field access - we're under lock
        var oldEntries = _entries;
        var oldRandomParts = _randomParts;

        // 2. Rehash
        for (int i = 0; i < oldEntries.Length; i++)
        {
            SegmentReference e = oldEntries[i];
            if (e.IsValid()) // Filters Empty AND Tombstones
            {
                var h = _store.ToGhostHeaderPointer(e);
                if (h != null)
                {
                    // Slot uses specific bit-range
                    int index = h->Id.SlotComputation & newMask;
                    while (!newEntries[index].IsEmpty())
                    {
                        index = (index + 1) & newMask;
                    }
                    newEntries[index] = e;
                    newRandomParts[index] = oldRandomParts[i]; // Preserve cached random part
                }
            }
        }

        // 3. Update direct fields for mutators
        _entries = newEntries;
        _randomParts = newRandomParts;
        _mask = newMask;
        _capacity = newCapacity;
        _tombstoneCount = 0;
        _occupied = _count;
        UpdateThresholds();

        // 4. Atomic publish for lock-free readers (single reference swap)
        _state = new MapState(newEntries, newRandomParts, newMask);
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

    public Enumerator GetEnumerator() => new Enumerator(_state);

    public struct Enumerator
    {
        private readonly SegmentReference[] _entries;
        private int _index;
        private SegmentReference _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(MapState state)
        {
            // Atomic capture of consistent state for lock-free iteration
            _entries = state.Entries;
            _index = 0;
            _current = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var arr = _entries;
            int len = arr.Length;
            int i = _index;

            while (i < len)
            {
                var val = arr[i];
                i++;

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

        public SegmentReference Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }

    /// <summary>
    /// Returns an enumerator that yields ONLY the latest version of each key visible at maxTxnId.
    /// </summary>
    public DeduplicatedEnumerator GetDeduplicatedEnumerator(long maxTxnId)
        => new DeduplicatedEnumerator(_state, _store, maxTxnId);

    public struct DeduplicatedEnumerator
    {
        private readonly SegmentReference[] _entries;
        private readonly short[] _randomParts;
        private readonly ISegmentStore _store;
        private readonly long _maxTxnId;
        private readonly int _mask;

        private int _index;
        private SegmentReference _current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal DeduplicatedEnumerator(MapState state, ISegmentStore store, long maxTxnId)
        {
            // Atomic capture of consistent state for lock-free iteration
            _entries = state.Entries;
            _randomParts = state.RandomParts;
            _mask = state.Mask;
            _store = store;
            _maxTxnId = maxTxnId;
            _index = 0;
            _current = default;
        }

        public bool MoveNext()
        {
            var entries = _entries;
            var randomParts = _randomParts;
            var store = _store;
            long maxTxn = _maxTxnId;
            int mask = _mask;
            int len = entries.Length;

            while (_index < len)
            {
                SegmentReference candidate = entries[_index];
                short candidateRandomPart = randomParts[_index];
                _index++;

                if (!candidate.IsValid()) continue;

                // 1. Check Visibility
                GhostHeader* h = store.ToGhostHeaderPointer(candidate);
                if (h == null || h->TxnId > maxTxn) continue;

                // 2. The "Winner Check" (Deduplication)
                if (IsBestVersion(h->Id, candidateRandomPart, candidate, entries, randomParts, mask, maxTxn, store))
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
        /// Uses cached random parts for fast filtering.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBestVersion(
            GhostId id,
            short searchRandomPart,
            SegmentReference candidate,
            SegmentReference[] entries,
            short[] randomParts,
            int mask,
            long maxTxnId,
            ISegmentStore store)
        {
            // Slot uses specific bit-range
            int index = id.SlotComputation & mask;

            SegmentReference bestRef = default;
            long bestTxnFound = long.MinValue;

            while (true)
            {
                SegmentReference current = entries[index];

                if (current.IsEmpty())
                    break;

                if (!current.IsTombstone())
                {
                    // Fast filter: compare cached random part first
                    if (randomParts[index] == searchRandomPart)
                    {
                        var h = store.ToGhostHeaderPointer(current);
                        if (h != null && h->Id == id)
                        {
                            long txnId = h->TxnId;
                            if (txnId <= maxTxnId)
                            {
                                if (txnId > bestTxnFound)
                                {
                                    bestTxnFound = txnId;
                                    bestRef = current;
                                }
                            }
                        }
                    }
                }

                index = (index + 1) & mask;
            }

            return bestRef.Value == candidate.Value;
        }

        public SegmentReference Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }
    }
}