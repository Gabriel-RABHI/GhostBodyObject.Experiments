using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Helpers;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static GhostBodyObject.Repository.Tests.Repository.Index.SegmentGhostTransactionnalMapShould;

namespace GhostBodyObject.Repository.Tests.Repository.Index
{
    public unsafe class SegmentGhostMapSetAndRemoveShould
    {
        [Fact]
        public void Prune_Superseeded_Versions_Single_Prune()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 1, 1, 1);

            // Setup: V10, V20, V30
            // We use standard Set() first to populate
            var r10 = store.NewHeader(id, 10);
            var r20 = store.NewHeader(id, 20);
            var r30 = store.NewHeader(id, 30);

            store.Update(map);

            Assert.Equal(3, map.Count);

            // Insert V40 with BottomTxnId = 25
            // Scenario:
            // Existing: 10, 20, 30.
            // New Bottom: 25.
            // Visible at 25: 20 (Txn 20).
            // 30 is > 25 (Keep).
            // 20 is <= 25 (Keep as Best Old).
            // 10 is < 20 (Prune).

            var r40 = store.NewHeader(id, 40);

            fixed (GhostHeader* h = &r40.Header)
            {
                map.SetAndRemove(r40.Reference, h, 25);
            }

            // Expected Count: 3
            // Kept: 20, 30, 40.
            // Removed: 10.
            Assert.Equal(3, map.Count);

            // Verify Logic
            // V40 present
            Assert.True(map.Get(id, 40, out var res40));
            Assert.Equal(40, store.ToGhostHeaderPointer(res40)->TxnId);

            // V30 present
            Assert.True(map.Get(id, 30, out var res30));
            Assert.Equal(30, store.ToGhostHeaderPointer(res30)->TxnId);

            // V20 present (Best Old)
            Assert.True(map.Get(id, 25, out var res25));
            Assert.Equal(20, store.ToGhostHeaderPointer(res25)->TxnId);

            // V10 gone
            Assert.False(map.Get(id, 15, out _));
        }

        [Fact]
        public void Prune_Multiple_Old_Versions()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 2, 2, 2);

            // Setup: 5, 10, 15, 20
            store.NewHeader(id, 5);
            store.NewHeader(id, 10);
            store.NewHeader(id, 15);
            store.NewHeader(id, 20);
            store.Update(map);

            Assert.Equal(4, map.Count);

            // Insert V30, Bottom = 18.
            // Keep: 30 (New), 20 (>18).
            // Best Old (<=18): 15.
            // Prune: 5, 10.

            var r30 = store.NewHeader(id, 30);
            fixed (GhostHeader* h = &r30.Header)
            {
                map.SetAndRemove(r30.Reference, h, 18);
            }

            // Expected Count: 3 (30, 20, 15)
            // Removed 2 (5, 10), Added 1 (30). Net -1. 4 -> 3.
            Assert.Equal(3, map.Count);

            Assert.True(map.Get(id, 30, out _));
            Assert.True(map.Get(id, 20, out _));

            // Check 15 is there
            Assert.True(map.Get(id, 18, out var res18));
            Assert.Equal(15, store.ToGhostHeaderPointer(res18)->TxnId);

            // Check 10 is gone
            Assert.False(map.Get(id, 12, out _));
        }

        [Fact]
        public void Recycle_Garbage_Slot_For_Insertion()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 3, 3, 3);

            // Setup: 10, 20.
            store.NewHeader(id, 10);
            store.NewHeader(id, 20);
            store.Update(map);
            Assert.Equal(2, map.Count);

            // Insert 30, Bottom = 25.
            // 20 is <= 25 (Best Old).
            // 10 is < 20 (Garbage).
            // 10 should be recycled for 30.

            var r30 = store.NewHeader(id, 30);
            fixed (GhostHeader* h = &r30.Header)
            {
                map.SetAndRemove(r30.Reference, h, 25);
            }

            // Count should remain 2. (Removed 10, Added 30).
            Assert.Equal(2, map.Count);

            Assert.True(map.Get(id, 30, out _));
            Assert.True(map.Get(id, 20, out _));
            Assert.False(map.Get(id, 15, out _));
        }

        [Fact]
        public void Handle_Insertion_Already_Exists()
        {
            // Case where we try to insert V20, but V20 exists.
            // We should still prune older versions if applicable.

            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 4, 4, 4);

            store.NewHeader(id, 10);
            store.NewHeader(id, 20);
            store.Update(map);

            // Insert V20 again, Bottom = 25.
            // 20 exists. Update pending = false.
            // 20 is <= 25 (Best Old).
            // 10 is < 20 (Garbage).
            // 10 should be removed (Tombstone).

            // We need a NEW record for V20 to pass to SetAndRemove, 
            // but physically it's the same logical version.
            var r20Duplicate = store.NewHeader(id, 20);
            // Change reference value slightly to ensure it's not "Exact Reference Match"
            r20Duplicate.Reference.Value += 1;

            fixed (GhostHeader* h = &r20Duplicate.Header)
            {
                map.SetAndRemove(r20Duplicate.Reference, h, 25);
            }

            // Count: 2 -> 1 (Removed 10). 20 Updated.
            Assert.Equal(1, map.Count);

            Assert.True(map.Get(id, 20, out var res));
            Assert.Equal(r20Duplicate.Reference.Value, res.Value);

            Assert.False(map.Get(id, 15, out _));
        }

        [Fact]
        public void Handle_New_Version_Is_Old_Backfill()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 5, 5, 5);

            store.NewHeader(id, 10);
            store.NewHeader(id, 20);
            store.Update(map);

            var r15 = store.NewHeader(id, 15);
            fixed (GhostHeader* h = &r15.Header)
            {
                map.SetAndRemove(r15.Reference, h, 25);
            }

            // We expect 15 to be inserted (replacing 10).
            Assert.True(map.Get(id, 18, out var res18));
            Assert.Equal(15, store.ToGhostHeaderPointer(res18)->TxnId);

            Assert.True(map.Get(id, 25, out var res25));
            Assert.Equal(20, store.ToGhostHeaderPointer(res25)->TxnId);
        }

        [Fact]
        public void Verify_Segment_Usage_Counting()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 6, 6, 6);

            // 1. Initial Insert (Set)
            var r10 = store.NewHeader(id, 10);
            // r10.Reference.SegmentId is 0 by default in FakeStore
            store.Update(map);

            // Usage of Segment 0 should be 1
            Assert.Equal(1, store.UsageCounts[0]);

            // 2. Second Insert (Set)
            var r20 = store.NewHeader(id, 20);
            store.Update(map);

            // Usage of Segment 0 should be 2
            Assert.Equal(2, store.UsageCounts[0]);

            // 3. SetAndRemove - Insert New, Prune Old
            // Insert V30, Bottom = 25.
            // V10 (Txn 10) <= 25.
            // V20 (Txn 20) <= 25. V20 > V10.
            // V10 is Garbage.
            // V10 should be recycled for V30.
            // Net result: V10 removed (Dec), V30 added (Inc).
            // Usage should remain 2.

            var r30 = store.NewHeader(id, 30);
            fixed (GhostHeader* h = &r30.Header)
            {
                map.SetAndRemove(r30.Reference, h, 25);
            }

            Assert.Equal(2, store.UsageCounts[0]);

            // 4. SetAndRemove - No Pruning (Pure Insert)
            // Insert V40, Bottom = 25.
            // V20 <= 25 (Best Old).
            // V30 > 25.
            // V40 > 25.
            // No garbage.
            // V40 added. Usage -> 3.

            var r40 = store.NewHeader(id, 40);
            fixed (GhostHeader* h = &r40.Header)
            {
                map.SetAndRemove(r40.Reference, h, 25);
            }

            Assert.Equal(3, store.UsageCounts[0]);

            // 5. Remove (Explicit)
            map.Remove(id, 40); // Remove V40
            // Usage -> 2
            Assert.Equal(2, store.UsageCounts[0]);
        }

        [Fact]
        public void Verify_Prune_Method()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var idA = new GhostId(GhostIdKind.Entity, 7, 7, 7);
            var idB = new GhostId(GhostIdKind.Entity, 8, 8, 8);

            // A: 10, 20
            store.NewHeader(idA, 10);
            store.NewHeader(idA, 20);

            // B: 5, 15
            store.NewHeader(idB, 5);
            store.NewHeader(idB, 15);

            store.Update(map);

            // Initial State: 4 items. Usage[0] = 4.
            Assert.Equal(4, map.Count);
            Assert.Equal(4, store.UsageCounts[0]);

            // Prune with Bottom = 18.
            // A: 20 > 18 (Keep). 10 <= 18. Best Old = 10. Kept.
            // B: 15 <= 18. 5 <= 18. 15 > 5. Keep 15. Remove 5.

            int removed = map.Prune(18);

            Assert.Equal(1, removed);
            Assert.Equal(3, map.Count);
            Assert.Equal(3, store.UsageCounts[0]);

            Assert.True(map.Get(idA, 10, out _));
            Assert.True(map.Get(idA, 20, out _));
            Assert.True(map.Get(idB, 15, out _));
            Assert.False(map.Get(idB, 5, out _));
        }

        [Fact]
        public void ConcurrentUpdatesAndEnumeration_ShouldConsistency()
        {
            using var store = new ThreadSafeFakeSegmentStore();
            var map = new SegmentGhostMap<ThreadSafeFakeSegmentStore>(store, initialCapacity: 16);
            var id = new GhostId(GhostIdKind.Entity, 99, 99, 99);

            long globalTxnId = 0;
            bool running = true;
            var truth = new ConcurrentDictionary<long, SegmentReference>();

            // initialize with one record
            {
                long txnId = Interlocked.Increment(ref globalTxnId);
                var r = store.NewHeader(id, txnId, out var h);
                truth[txnId] = r;
                map.Set(r, h);
            }

            var tasks = new List<Task>();

            // 1. Updater Threads
            // Continually adds newer versions and prunes very old ones
            int updaterCount = 4; // Multiple setters
            for (int i = 0; i < updaterCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var rnd = new Random(12345 + i);
                    while (Volatile.Read(ref running))
                    {
                        long newTxnId = Interlocked.Increment(ref globalTxnId);
                        var r = store.NewHeader(id, newTxnId, out var h);
                        truth[newTxnId] = r;

                        // Keep last 50 versions visible, prune older
                        long bottomTxnId = Math.Max(0, newTxnId - 50);

                        map.SetAndRemove(r, h, bottomTxnId);

                        // Small spin to vary load
                        if (rnd.Next(10) == 0) Thread.Yield();
                    }
                }));
            }

            // 2. Reader Threads (Enumerators)
            // They pick a random time T and verify map returns a valid version V <= T
            int readerCount = 4;
            for (int i = 0; i < readerCount; i++)
            {
                int seed = i * 999;
                tasks.Add(Task.Run(() =>
                {
                    var rnd = new Random(seed);
                    while (Volatile.Read(ref running))
                    {
                        long maxTxn = Interlocked.Read(ref globalTxnId);
                        long queryTxn = (long)(rnd.NextDouble() * maxTxn) + 1;

                        if (map.Get(id, queryTxn, out var foundRef))
                        {
                            var ptr = store.ToGhostHeaderPointer(foundRef);

                            // Safety Check 1: Pointer valid
                            if (ptr == null)
                                throw new Exception("Map returned reference to null pointer");

                            long foundTxn = ptr->TxnId;

                            // Safety Check 2: Consistency with time constraint
                            if (foundTxn > queryTxn)
                                throw new Exception($"Found TxnId {foundTxn} which is > Query TxnId {queryTxn}");

                            // Safety Check 3: Data Integrity (Reference matches Truth)
                            if (truth.TryGetValue(foundTxn, out var expectedRef))
                            {
                                if (expectedRef.Value != foundRef.Value)
                                    throw new Exception($"Data Corruption! For TxnId {foundTxn}, expected Ref {expectedRef.Value} but got {foundRef.Value}");
                            }
                            else
                            {
                                // If not in truth, we have a problem (we never delete from truth in this test)
                                throw new Exception($"Unknown TxnId {foundTxn} found in map");
                            }
                        }
                    }
                }));
            }

            // Run for 2 seconds
            Thread.Sleep(5 * 1000);
            Volatile.Write(ref running, false);
            Task.WaitAll(tasks.ToArray());

            Assert.True(map.Count > 0);
        }

        [Fact]
        public void ConcurrentUpdatesAndDeduplicatedEnumeration_CoherencyCheck()
        {
            using var store = new ThreadSafeFakeSegmentStore();
            var map = new SegmentGhostMap<ThreadSafeFakeSegmentStore>(store, initialCapacity: 16);
            var id = new GhostId(GhostIdKind.Entity, 77, 77, 77);
            var ranges = new GhostRepositoryTransactionIdRange();

            bool running = true;

            // initialize with one record
            {
                long txnId = ranges.IncrementTopTransactionId();
                var r = store.NewHeader(id, txnId, out var h);
                map.Set(r, h);
            }

            var tasks = new List<Task>();

            int readerCount = 4;

            // 1. Updater Thread (Single Writer Rule)
            int updaterCount = 1;
            for (int i = 0; i < updaterCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var rnd = new Random(12345 + i);
                    var mutationCount = 0;
                    while (Volatile.Read(ref running))
                    {
                        long newTxnId = ranges.TopTransactionId + 1;
                        var r = store.NewHeader(id, newTxnId, out var h);
                        map.SetAndRemove(r, h, ranges.BottomTransactionId);
                        ranges.IncrementTopTransactionId();
                        if (rnd.Next(10) == 0)
                            Thread.Yield();
                        mutationCount++;
                    }
                    Console.WriteLine($"Update count = {mutationCount}");
                }));
            }

            // 2. Reader Threads (Deduplicated Enumerator)
            for (int i = 0; i < readerCount; i++)
            {
                int seed = i * 999;
                tasks.Add(Task.Run(() =>
                {
                    bool seenAny = false;
                    var rnd = new Random(seed);
                    var totalFound = 0;
                    while (Volatile.Read(ref running))
                    {
                        var count = 0;
                        var txnId = ranges.AddTransactionViewer();
                        var enumerator = map.GetDeduplicatedEnumerator(txnId);
                        var foundRef = SegmentReference.Empty;

                        while (enumerator.MoveNext())
                        {
                            seenAny = true;
                            var currentRef = enumerator.Current;
                            var ptr = store.ToGhostHeaderPointer(currentRef);

                            if (ptr == null)
                                throw new Exception("Enumerator returned reference to null pointer");

                            foundRef = currentRef;
                            if (ptr->Id == id)
                            {
                                count++;
                                totalFound++;
                            }
                            else
                            {
                                throw new Exception("Enumerator returned unknown ID.");
                            }
                        }
                        ranges.RemoveTransactionViewer(txnId);
                        if (count > 1)
                            throw new Exception($"DeduplicatedEnumerator returned {count} items for the same ID!");

                        if (seenAny && count == 0)
                            throw new Exception($"DeduplicatedEnumerator do not sees the ID! txnId = {txnId}, ranges.BottomTransactionId={ranges.BottomTransactionId}, ranges.TopTransactionId={ranges.TopTransactionId}");


                        if (count == 1)
                        {
                            var ptr = store.ToGhostHeaderPointer(foundRef);
                            long foundTxn = ptr->TxnId;

                            if (foundTxn > txnId)
                                throw new Exception($"Enumerator found foundTxn {foundTxn} > Query txnId {txnId}");
                        }
                    }
                    Console.WriteLine($"Reader {seed} found total {totalFound} items.");
                }));
            }

            // Run for 2 seconds
            Thread.Sleep(5 * 1000);
            Volatile.Write(ref running, false);
            Task.WaitAll(tasks.ToArray());

            Assert.True(map.Count > 0);
        }


        public class ThreadSafeFakeSegmentStore : ISegmentStore, IDisposable
        {
            private ConcurrentDictionary<ulong, IntPtr> _pointers = new();
            private ConcurrentDictionary<uint, int> _usageCounts = new();
            private long _nextOffset = 0;

            public SegmentReference NewHeader(GhostId id, long txnId, out GhostHeader* h)
            {
                long offset = Interlocked.Increment(ref _nextOffset);
                var r = new SegmentReference { SegmentId = 0, Offset = (uint)offset }; // Unique by offset

                // Use a derived value to ensure uniqueness if needed, but offset is unique.
                // However, SegmentReference layout: Value is 64 bit. 
                // SegmentId (32) | Offset (32).
                // Offset increment ensures unique Value.

                IntPtr p = Marshal.AllocHGlobal(sizeof(GhostHeader));
                h = (GhostHeader*)p;
                h->Id = id;
                h->TxnId = txnId;

                _pointers[r.Value] = p;
                return r;
            }

            public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            {
                if (_pointers.TryGetValue(reference.Value, out IntPtr ptr))
                {
                    return (GhostHeader*)ptr;
                }
                return null;
            }

            public void IncrementSegmentHolderUsage(SegmentReference reference)
            {
                _usageCounts.AddOrUpdate(reference.SegmentId, 1, (k, v) => v + 1);
            }

            public void DecrementSegmentHolderUsage(SegmentReference reference)
            {
                _usageCounts.AddOrUpdate(reference.SegmentId, 0, (k, v) => Math.Max(0, v - 1));
            }

            public void Dispose()
            {
                foreach (var ptr in _pointers.Values)
                {
                    Marshal.FreeHGlobal(ptr);
                }
                _pointers.Clear();
            }

            // Unused / Throwing
            public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId) => throw new NotImplementedException();
            public SegmentReference StoreGhost(long bottomTxnId, PinnedMemory<byte> ghost, long txnId) => throw new NotImplementedException();
            public bool WriteTransaction<T>(T commiter, long txnId, Action<GhostId, SegmentReference> onGhostStored) where T : IModifiedBodyStream => throw new NotImplementedException();
        }
    }
}
