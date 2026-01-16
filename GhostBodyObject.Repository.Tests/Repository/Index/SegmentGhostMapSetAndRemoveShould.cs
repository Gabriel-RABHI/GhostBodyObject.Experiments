using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Structs;
using System.Collections.Generic;
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
    }
}
