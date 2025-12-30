using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Benchmarks.Ghosts
{
    public unsafe class SegmentGhostTransactionnalMapBenchmarks : BenchmarkBase
    {
        private const int COUNT = 1_000_000;

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

        [BruteForceBenchmark("RP-01", "Measure performance for SegmentGhostTransactionnalMapBenchmarks", "Index")]
        public void SequentialTest()
        {
            var fakeStore = new FakeSegmentStore();
            var map = new SegmentGhostTransactionnalMap(fakeStore);
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                }
            })
            .PrintToConsole($"Insert {COUNT:N0} entries")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();
        }
    }
}
