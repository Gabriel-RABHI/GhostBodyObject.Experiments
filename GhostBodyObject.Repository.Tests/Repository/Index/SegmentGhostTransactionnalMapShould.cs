using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

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

            public void Update(SegmentGhostTransactionnalMap map)
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
        }

        // ---------------------------------------------------------
        // TESTS
        // ---------------------------------------------------------

        [Fact]
        public void Store_Multiple_Unique_Headers()
        {
            var store = new FakeSegmentStore();
            var map = new SegmentGhostTransactionnalMap(store);

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
            var map = new SegmentGhostTransactionnalMap(store);

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
            var map = new SegmentGhostTransactionnalMap(store);
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
            var map = new SegmentGhostTransactionnalMap(store);
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
            var map = new SegmentGhostTransactionnalMap(store, initialCapacity: 16);
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
            var map = new SegmentGhostTransactionnalMap(store, initialCapacity: 16);
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
            var map = new SegmentGhostTransactionnalMap(store);
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
            var map = new SegmentGhostTransactionnalMap(store, initialCapacity: 4);

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
            var map = new SegmentGhostTransactionnalMap(store);

            var id = new GhostId(GhostIdKind.Entity, 99, 99, 99);
            Assert.False(map.Get(id, 100, out _));
        }
    }
}