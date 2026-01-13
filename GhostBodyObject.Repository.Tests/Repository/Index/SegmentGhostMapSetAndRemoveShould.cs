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
            // Insert V15. Bottom = 25.
            // Exists: 10, 20.
            // 20 <= 25 (Best Old).
            // 15 <= 20. 15 is Garbage immediately?
            // Or 15 replaces 10?
            // 10 < 20. 10 is Garbage.
            // 15 < 20. 15 is Garbage.
            // So if we insert 15, it should effectively be discarded or replace a garbage slot and then be discarded?
            // "SetAndRemove" implies we SET it. 
            // If we Set it, we might overwrite a slot.
            // Our logic:
            // Scan 10. <= 25. BestOld = 10.
            // Scan 20. <= 25. 20 > 10. BestOld = 20. 10 is Garbage.
            // Recycle 10 for V15?
            // If we recycle 10 for V15.
            // Map has: 15, 20.
            // But 15 < 20. So 15 is also garbage relative to 20.
            // But the loop doesn't check the *just inserted* value against BestOld.
            // So 15 stays?
            // If 15 stays, map has 15, 20.
            // Reader at 25 sees 20.
            // Reader at 18 sees 15.
            // Wait. If 20 exists, and we insert 15.
            // Is 15 valid?
            // Yes, for history [15, 20).
            // But we said we only keep ONE old version.
            // If Bottom = 25.
            // Range (-inf, 25] is the "Old" zone.
            // We only need the version covering 25. That is 20.
            // 15 is not needed for 25.
            // Is it needed for [15, 20)?
            // The contract says: "remove entries that are no longer available for the oldest transaction".
            // Oldest transaction = Bottom = 25.
            // So we only care about queries >= 25.
            // Query at 25 -> Needs 20.
            // Query at 26 -> Needs 20.
            // No active transaction is < 25.
            // So 15 is useless.
            // So ideally 15 should NOT be in the map.
            // Does our code remove it?
            // If we insert V15.
            // Code: Loop finds 10, 20.
            // 10 becomes garbage. Recycled for 15.
            // 20 becomes BestOld.
            // Loop finishes.
            // Result: 15, 20.
            // 15 persists.
            // This is a slight imperfection (we inserted garbage), but valid safe behavior.
            // It will be cleaned up on NEXT SetAndRemove.
            
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
    }
}
