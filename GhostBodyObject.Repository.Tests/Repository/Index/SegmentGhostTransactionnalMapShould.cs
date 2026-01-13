using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static GhostBodyObject.Repository.Tests.Repository.Index.SegmentGhostTransactionnalMapShould;

namespace GhostBodyObject.Repository.Tests.Repository.Index
{
    public unsafe class SegmentGhostTransactionnalMapShould
    {
        // ---------------------------------------------------------
        // FAKE STORE IMPLEMENTATION (Reused & Tweaked)
        // ---------------------------------------------------------
        public class FakeSegmentStore : ISegmentStore
        {
            public class FakeSegmentStoreRecord
            {
                public SegmentReference Reference;
                public GhostHeader Header;
                public bool ToInsert;
                public bool IsInserted;
                public bool ToDelete;
                public bool IsDeleted;
            }

            private int _nextOffset = 0;
            private List<FakeSegmentStoreRecord> _store = new();
            // We pin headers here to stop GC moving them, ensuring pointer safety during tests
            private List<System.Runtime.InteropServices.GCHandle> _handles = new();

            public void Dispose()
            {
                foreach (var h in _handles) if (h.IsAllocated) h.Free();
            }

            public FakeSegmentStoreRecord NewHeader(long txnId)
            {
                // Generate a random ID
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)(++_nextOffset), XorShift64.Next());
                return CreateRecord(id, txnId);
            }

            public FakeSegmentStoreRecord NewHeader(GhostId id, long txnId)
            {
                ++_nextOffset;
                return CreateRecord(id, txnId);
            }

            private FakeSegmentStoreRecord CreateRecord(GhostId id, long txnId)
            {
                var h = new GhostHeader { Id = id, TxnId = txnId };

                var record = new FakeSegmentStoreRecord
                {
                    Reference = new SegmentReference { SegmentId = 0, Offset = (uint)_nextOffset },
                    Header = h,
                    ToInsert = true
                };

                _store.Add(record);
                return record;
            }

            public void MarkForDeletion(GhostId id, long txnId)
            {
                var record = _store.Find(r => r.Header.Id == id && r.Header.TxnId == txnId);
                if (record != null) record.ToDelete = true;
            }

            public unsafe GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            {
                // Simulate pointer access. In a real scenario, this would point to unmanaged memory.
                // For unit tests, we find the struct in our list.
                // Note: This is UNSAFE in a managed list if the list resizes, but acceptable for sequential unit tests.
                for (int i = 0; i < _store.Count; i++)
                {
                    if (_store[i].Reference.SegmentId == reference.SegmentId &&
                        _store[i].Reference.Offset == reference.Offset)
                    {
                        fixed (GhostHeader* p = &_store[i].Header) return p;
                    }
                }
                return null;
            }
            
            public SegmentReference StoreGhost(PinnedMemory<byte> ghost)
            {
                throw new NotImplementedException();
            }

            public void Update<TSegmentStore>(SegmentGhostMap<TSegmentStore> map)
                where TSegmentStore : ISegmentStore
            {
                foreach (var r in _store)
                {
                    // Handle Insertions
                    if (r.ToInsert && !r.IsInserted)
                    {
                        fixed (GhostHeader* p = &r.Header)
                        {
                            map.Set(r.Reference, p);
                        }
                        r.IsInserted = true;
                    }

                    // Handle Deletions
                    if (r.ToDelete && !r.IsDeleted)
                    {
                        map.Remove(r.Header.Id, r.Header.TxnId);
                        r.IsDeleted = true;
                    }
                }
            }

            public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
            {
                throw new NotImplementedException();
            }

            public Dictionary<uint, int> UsageCounts = new();

            public void IncrementSegmentHolderUsage(uint segmentId)
            {
                if (!UsageCounts.ContainsKey(segmentId))
                    UsageCounts[segmentId] = 0;
                UsageCounts[segmentId]++;
            }

            public void DecrementSegmentHolderUsage(uint segmentId)
            {
                if (!UsageCounts.ContainsKey(segmentId))
                    UsageCounts[segmentId] = 0;
                UsageCounts[segmentId]--;
            }
        }

        // ---------------------------------------------------------
        // TESTS
        // ---------------------------------------------------------

        [Fact]
        public void Store_Multiple_Unique_Headers()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);

            store.NewHeader(10);
            store.NewHeader(10);
            store.NewHeader(10);

            store.Update(map);

            Assert.Equal(3, map.Count);
        }

        [Fact]
        public void Retrieve_Correct_Version_By_TransactionId()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);

            var id = new GhostId(GhostIdKind.Entity, 1, 1, 1);

            // Create 3 versions of the SAME ID
            store.NewHeader(id, 10); // Version A
            store.NewHeader(id, 20); // Version B
            store.NewHeader(id, 30); // Version C

            store.Update(map);

            // 1. Query at Txn 15 -> Should get Version A (Txn 10)
            bool found15 = map.Get(id, 15, out var res15);
            Assert.True(found15);
            Assert.Equal(10, store.ToGhostHeaderPointer(res15)->TxnId);

            // 2. Query at Txn 25 -> Should get Version B (Txn 20)
            bool found25 = map.Get(id, 25, out var res25);
            Assert.True(found25);
            Assert.Equal(20, store.ToGhostHeaderPointer(res25)->TxnId);

            // 3. Query at Txn 5 -> Should find NOTHING (Txn 10 is too new)
            bool found5 = map.Get(id, 5, out var res5);
            Assert.False(found5);
        }

        [Fact]
        public void Handle_Duplicates_Idempotency()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 1, 1, 1);

            // Add the EXACT same header (ID + TxnId) twice
            store.NewHeader(id, 10);
            store.Update(map);

            store.NewHeader(id, 10); // Duplicate
            store.Update(map);

            // Count should still be 1 because Set() detects exact duplicate references
            Assert.Equal(1, map.Count);
        }

        [Fact]
        public void Remove_Specific_Version()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var id = new GhostId(GhostIdKind.Entity, 1, 1, 1);

            store.NewHeader(id, 10); // v1
            store.NewHeader(id, 20); // v2
            store.Update(map);

            Assert.Equal(2, map.Count);

            // Delete v1
            store.MarkForDeletion(id, 10);
            store.Update(map);

            Assert.Equal(1, map.Count);

            // Verify v1 is gone but v2 remains
            Assert.False(map.Get(id, 15, out _)); // Searching < 20 fails
            Assert.True(map.Get(id, 25, out var res));
            Assert.Equal(20, store.ToGhostHeaderPointer(res)->TxnId);
        }

        [Fact]
        public void Resize_When_Capacity_Exceeded()
        {
            var store = new FakeSegmentStore();
            // Start small (Capacity 16)
            var map = new SegmentGhostMap<FakeSegmentStore>(store, initialCapacity: 16);
            int itemsToAdd = 100;

            var ids = new List<GhostId>();

            for (int i = 0; i < itemsToAdd; i++)
            {
                var rec = store.NewHeader(10);
                ids.Add(rec.Header.Id);
            }

            store.Update(map);

            // Verify all items are present
            Assert.Equal(itemsToAdd, map.Count);

            // Check random item existence
            foreach (var id in ids)
            {
                Assert.True(map.Get(id, 20, out _));
            }
        }

        [Fact]
        public void Reuse_Tombstones()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store, initialCapacity: 16);
            var id = new GhostId(GhostIdKind.Entity, 1, 1, 1);

            // 1. Insert
            store.NewHeader(id, 10);
            store.Update(map);
            Assert.Equal(1, map.Count);

            // 2. Remove (Creates Tombstone)
            store.MarkForDeletion(id, 10);
            store.Update(map);
            Assert.Equal(0, map.Count);

            // 3. Insert New Item (Should reuse the slot)
            var id2 = new GhostId(GhostIdKind.Entity, 2, 2, 2);
            // We force a hash collision for test purposes strictly if we mocked hash, 
            // but here we just rely on the map filling up or reusing the first available slot 
            // if the hash lands there. 
            // For a black-box test, we just ensure the count goes back up.

            store.NewHeader(id2, 10);
            store.Update(map);
            Assert.Equal(1, map.Count);
        }

        [Fact]
        public void Deduplication_Enumerator_Winner_Takes_All()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);
            var idA = new GhostId(GhostIdKind.Entity, 1, 1, 1);
            var idB = new GhostId(GhostIdKind.Entity, 2, 2, 2);

            // ID A: Versions 10, 20, 30
            store.NewHeader(idA, 10);
            store.NewHeader(idA, 20);
            store.NewHeader(idA, 30);

            // ID B: Version 50
            store.NewHeader(idB, 50);

            store.Update(map);

            // TEST 1: Query at Txn 25
            // Should see: idA (v20), but NOT v10 (shadowed) or v30 (future). 
            // Should NOT see: idB (v50 is future).
            var results25 = new List<long>();
            var enumerator = map.GetDeduplicatedEnumerator(25);

            while (enumerator.MoveNext())
            {
                var ptr = store.ToGhostHeaderPointer(enumerator.Current);
                results25.Add(ptr->TxnId);
            }

            Assert.Single(results25); // Only one item
            Assert.Equal(20, results25[0]); // Must be version 20 of A

            // TEST 2: Query at Txn 100
            // Should see: idA (v30 - latest), idB (v50)
            var results100 = new List<long>();
            var enum100 = map.GetDeduplicatedEnumerator(100);

            while (enum100.MoveNext())
            {
                var ptr = store.ToGhostHeaderPointer(enum100.Current);
                results100.Add(ptr->TxnId);
            }

            Assert.Equal(2, results100.Count);
            Assert.Contains(30, results100);
            Assert.Contains(50, results100);
        }

        [Fact]
        public void Handle_Hash_Collisions_Linear_Probing()
        {
            var store = new FakeSegmentStore();
            // Start with tiny capacity to force collisions easily
            var map = new SegmentGhostMap<FakeSegmentStore>(store, initialCapacity: 4);

            // We cannot easily force hash collisions with random IDs, 
            // but filling the map > 1 item in a size 4 map guarantees interaction.
            var id1 = new GhostId(GhostIdKind.Entity, 0, 0, 1); // 1 & 3 = 1
            var id2 = new GhostId(GhostIdKind.Entity, 0, 0, 5); // 5 & 3 = 1 (Collision!)

            store.NewHeader(id1, 10);
            store.NewHeader(id2, 10);
            store.Update(map);

            Assert.Equal(2, map.Count);

            // Ensure we can retrieve both
            Assert.True(map.Get(id1, 10, out _));
            Assert.True(map.Get(id2, 10, out _));
        }

        [Fact]
        public void Return_False_On_Missing_Keys()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store);

            var id = new GhostId(GhostIdKind.Entity, 99, 99, 99);
            Assert.False(map.Get(id, 100, out _));
        }

        [Fact]
        public void Handle_Large_Insert_And_Chunked_Removal_With_Capacity_And_Visibility_Check()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store, initialCapacity: 16);

            const int totalItems = 500;
            const int chunkSize = 100;
            const long baseTxnId = 100;

            var records = new List<FakeSegmentStore.FakeSegmentStoreRecord>();

            // ---------------------------------------------------------
            // PHASE 1: Insert all items
            // ---------------------------------------------------------
            for (int i = 0; i < totalItems; i++)
            {
                var rec = store.NewHeader(baseTxnId + i);
                records.Add(rec);
            }
            store.Update(map);

            // Verify all items inserted
            Assert.Equal(totalItems, map.Count);

            // Capacity should have grown (started at 16, now holding 500 items)
            // With 0.75 load factor, capacity should be at least 500/0.75 ≈ 667 -> next power of 2 = 1024
            Assert.True(map.Capacity >= totalItems, $"Capacity {map.Capacity} should be >= {totalItems}");

            int initialCapacity = map.Capacity;

            // Verify all items are visible
            foreach (var rec in records)
            {
                bool found = map.Get(rec.Header.Id, baseTxnId + totalItems, out var result);
                Assert.True(found, $"Item with TxnId {rec.Header.TxnId} should be found");
                Assert.Equal(rec.Reference.Value, result.Value);
            }

            // ---------------------------------------------------------
            // PHASE 2: Remove items in chunks and verify state after each chunk
            // ---------------------------------------------------------
            int remainingItems = totalItems;

            for (int chunkStart = 0; chunkStart < totalItems; chunkStart += chunkSize)
            {
                int chunkEnd = Math.Min(chunkStart + chunkSize, totalItems);
                int itemsToRemoveInChunk = chunkEnd - chunkStart;

                // Mark chunk for deletion
                for (int i = chunkStart; i < chunkEnd; i++)
                {
                    store.MarkForDeletion(records[i].Header.Id, records[i].Header.TxnId);
                }
                store.Update(map);

                remainingItems -= itemsToRemoveInChunk;

                // Verify Count matches expected remaining
                Assert.Equal(remainingItems, map.Count);

                // Verify removed items are no longer visible
                for (int i = chunkStart; i < chunkEnd; i++)
                {
                    bool found = map.Get(records[i].Header.Id, baseTxnId + totalItems, out _);
                    Assert.False(found, $"Removed item at index {i} should NOT be found");
                }

                // Verify remaining items are still visible
                for (int i = chunkEnd; i < totalItems; i++)
                {
                    bool found = map.Get(records[i].Header.Id, baseTxnId + totalItems, out var result);
                    Assert.True(found, $"Remaining item at index {i} should still be found");
                    Assert.Equal(records[i].Reference.Value, result.Value);
                }

                // Capacity check: should shrink when count drops significantly
                // The map shrinks when tombstones accumulate or count drops below thresholds
                if (remainingItems == 0)
                {
                    // When all items are removed, capacity might shrink to minimum
                    Assert.True(map.Capacity <= initialCapacity, 
                        $"Capacity {map.Capacity} should be <= initial {initialCapacity} after all removals");
                }
            }

            // ---------------------------------------------------------
            // PHASE 3: Final state verification
            // ---------------------------------------------------------
            Assert.Equal(0, map.Count);

            // Verify capacity has potentially shrunk (or at least not grown)
            Assert.True(map.Capacity <= initialCapacity, 
                $"Final capacity {map.Capacity} should be <= initial {initialCapacity}");

            // Verify no items are visible via enumerator
            var enumerator = map.GetEnumerator();
            int enumCount = 0;
            while (enumerator.MoveNext())
            {
                enumCount++;
            }
            Assert.Equal(0, enumCount);

            // Verify deduplicated enumerator also returns nothing
            var dedupEnumerator = map.GetDeduplicatedEnumerator(baseTxnId + totalItems);
            int dedupCount = 0;
            while (dedupEnumerator.MoveNext())
            {
                dedupCount++;
            }
            Assert.Equal(0, dedupCount);
        }

        [Fact]
        public void Handle_Mixed_Versions_During_Chunked_Removal()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostMap<FakeSegmentStore>(store, initialCapacity: 16);

            const int entityCount = 50;
            const int versionsPerEntity = 5;

            var entities = new List<GhostId>();
            var allRecords = new Dictionary<GhostId, List<FakeSegmentStore.FakeSegmentStoreRecord>>();

            // ---------------------------------------------------------
            // PHASE 1: Insert multiple versions per entity
            // ---------------------------------------------------------
            for (int e = 0; e < entityCount; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, (ushort)e, (ulong)e, XorShift64.Next());
                entities.Add(id);
                allRecords[id] = new List<FakeSegmentStore.FakeSegmentStoreRecord>();

                for (int v = 0; v < versionsPerEntity; v++)
                {
                    long txnId = (e * 100) + (v * 10); // e.g., entity 0: 0, 10, 20, 30, 40
                    var rec = store.NewHeader(id, txnId);
                    allRecords[id].Add(rec);
                }
            }
            store.Update(map);

            int totalRecords = entityCount * versionsPerEntity;
            Assert.Equal(totalRecords, map.Count);

            // ---------------------------------------------------------
            // PHASE 2: Verify visibility at different transaction points
            // ---------------------------------------------------------
            foreach (var id in entities)
            {
                var versions = allRecords[id];
                int entityIndex = entities.IndexOf(id);

                // Query at txnId that should see version 2 (txnId = entityIndex*100 + 20)
                long queryTxn = (entityIndex * 100) + 25; // Between version 2 (20) and version 3 (30)
                bool found = map.Get(id, queryTxn, out var result);
                Assert.True(found);

                var expectedTxnId = (entityIndex * 100) + 20; // Version 2
                Assert.Equal(expectedTxnId, store.ToGhostHeaderPointer(result)->TxnId);
            }

            // ---------------------------------------------------------
            // PHASE 3: Remove older versions (keep only latest 2 per entity)
            // ---------------------------------------------------------
            int removedCount = 0;
            foreach (var id in entities)
            {
                var versions = allRecords[id];
                // Remove versions 0, 1, 2 (keep 3 and 4)
                for (int v = 0; v < 3; v++)
                {
                    store.MarkForDeletion(versions[v].Header.Id, versions[v].Header.TxnId);
                    removedCount++;
                }
            }
            store.Update(map);

            int expectedRemaining = totalRecords - removedCount;
            Assert.Equal(expectedRemaining, map.Count);

            // ---------------------------------------------------------
            // PHASE 4: Verify visibility after partial removal
            // ---------------------------------------------------------
            foreach (var id in entities)
            {
                var versions = allRecords[id];
                int entityIndex = entities.IndexOf(id);

                // Query at txnId that would have seen version 2 before removal
                // Now should see version 3 (the lowest remaining)
                long queryTxn = (entityIndex * 100) + 35; // Between version 3 (30) and version 4 (40)
                bool found = map.Get(id, queryTxn, out var result);
                Assert.True(found);

                var expectedTxnId = (entityIndex * 100) + 30; // Version 3 (now lowest remaining)
                Assert.Equal(expectedTxnId, store.ToGhostHeaderPointer(result)->TxnId);

                // Query at very high txnId should see version 4 (latest)
                long highQueryTxn = (entityIndex * 100) + 100;
                found = map.Get(id, highQueryTxn, out result);
                Assert.True(found);

                expectedTxnId = (entityIndex * 100) + 40; // Version 4 (latest)
                Assert.Equal(expectedTxnId, store.ToGhostHeaderPointer(result)->TxnId);

                // Query at txnId before remaining versions should find nothing
                long lowQueryTxn = (entityIndex * 100) + 25; // Before version 3 (30)
                found = map.Get(id, lowQueryTxn, out _);
                Assert.False(found, "Should not find any version before remaining versions");
            }

            // ---------------------------------------------------------
            // PHASE 5: Remove all remaining and verify empty state
            // ---------------------------------------------------------
            foreach (var id in entities)
            {
                var versions = allRecords[id];
                // Remove versions 3 and 4
                for (int v = 3; v < versionsPerEntity; v++)
                {
                    store.MarkForDeletion(versions[v].Header.Id, versions[v].Header.TxnId);
                }
            }
            store.Update(map);

            Assert.Equal(0, map.Count);

            // Verify no entity is visible
            foreach (var id in entities)
            {
                bool found = map.Get(id, long.MaxValue, out _);
                Assert.False(found, "No entity should be visible after complete removal");
            }
        }
    }
}