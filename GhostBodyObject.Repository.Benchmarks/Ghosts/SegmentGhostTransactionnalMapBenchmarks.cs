using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Benchmarks.Ghosts
{
    public unsafe class SegmentGhostTransactionnalMapBenchmarks : BenchmarkBase
    {
        private const int COUNT = 10_000_000;

        // ---------------------------------------------------------
        // FAST PINNED STORE IMPLEMENTATION FOR BENCHMARKS
        // ---------------------------------------------------------
        public class PinnedSegmentStore : ISegmentStore, IDisposable
        {
            private GhostHeader[] _headers;
            private GCHandle _handle;
            private GhostHeader* _basePointer;

            public PinnedSegmentStore(int capacity)
            {
                _headers = new GhostHeader[capacity];
                _handle = GCHandle.Alloc(_headers, GCHandleType.Pinned);
                _basePointer = (GhostHeader*)_handle.AddrOfPinnedObject();
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                    _handle.Free();
            }

            public GhostHeader* GetHeaderPointer(int index) => _basePointer + index;

            public SegmentReference CreateReference(int index) => new SegmentReference
            {
                SegmentId = 0,
                Offset = (uint)(index * sizeof(GhostHeader))
            };

            public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
                => (GhostHeader*)((byte*)_basePointer + reference.Offset);
        }

        // ---------------------------------------------------------
        // PREPARED DATA STRUCTURE
        // ---------------------------------------------------------
        private struct PreparedEntry
        {
            public SegmentReference Reference;
            public GhostHeader* HeaderPointer;
        }

        [BruteForceBenchmark("RP-01", "Measure Set() insertion performance for SegmentGhostTransactionnalMap", "Index")]
        public void Benchmark_Set_Insertion()
        {
            using var store = new PinnedSegmentStore(COUNT);
            var map = new SegmentGhostTransactionnalMap(store);

            // Prepare all data outside the measured loop
            var entries = new PreparedEntry[COUNT];
            for (int i = 0; i < COUNT; i++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)i, XorShift64.Next());
                var headerPtr = store.GetHeaderPointer(i);
                headerPtr->Id = id;
                headerPtr->TxnId = i;

                entries[i] = new PreparedEntry
                {
                    Reference = store.CreateReference(i),
                    HeaderPointer = headerPtr
                };
            }

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {COUNT:N0} unique entries")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");
        }

        [BruteForceBenchmark("RP-02", "Measure Get() lookup performance for SegmentGhostTransactionnalMap", "Index")]
        public void Benchmark_Get_Lookup()
        {
            using var store = new PinnedSegmentStore(COUNT);
            var map = new SegmentGhostTransactionnalMap(store);

            // Prepare and insert all data
            var ids = new GhostId[COUNT];
            for (int i = 0; i < COUNT; i++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)i, XorShift64.Next());
                ids[i] = id;

                var headerPtr = store.GetHeaderPointer(i);
                headerPtr->Id = id;
                headerPtr->TxnId = i;

                map.Set(store.CreateReference(i), headerPtr);
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");

            long maxTxnId = COUNT + 1;

            // Measure only the Get() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    map.Get(ids[i], maxTxnId, out _);
                }
            })
            .PrintToConsole($"Get() {COUNT:N0} lookups")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-03", "Measure Set() with multiple versions per entity", "Index")]
        public void Benchmark_Set_MultipleVersions()
        {
            const int ENTITY_COUNT = 1_000_000;
            const int VERSIONS_PER_ENTITY = 10;
            int totalEntries = ENTITY_COUNT * VERSIONS_PER_ENTITY;

            using var store = new PinnedSegmentStore(totalEntries);
            var map = new SegmentGhostTransactionnalMap(store);

            // Prepare all data outside the measured loop
            var entries = new PreparedEntry[totalEntries];
            int entryIndex = 0;

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());

                for (int v = 0; v < VERSIONS_PER_ENTITY; v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = (e * 100) + v;

                    entries[entryIndex] = new PreparedEntry
                    {
                        Reference = store.CreateReference(entryIndex),
                        HeaderPointer = headerPtr
                    };
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < totalEntries; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {totalEntries:N0} entries ({ENTITY_COUNT:N0} entities x {VERSIONS_PER_ENTITY} versions)")
            .PrintDelayPerOp(totalEntries)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-04", "Measure Remove() performance for SegmentGhostTransactionnalMap", "Index")]
        public void Benchmark_Remove()
        {
            using var store = new PinnedSegmentStore(COUNT);
            var map = new SegmentGhostTransactionnalMap(store);

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");

            // Prepare and insert all data
            var removeData = new (GhostId Id, long TxnId)[COUNT];
            for (int i = 0; i < COUNT; i++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)i, XorShift64.Next());
                long txnId = i;
                removeData[i] = (id, txnId);

                var headerPtr = store.GetHeaderPointer(i);
                headerPtr->Id = id;
                headerPtr->TxnId = txnId;

                map.Set(store.CreateReference(i), headerPtr);
            }

            // Measure only the Remove() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    map.Remove(removeData[i].Id, removeData[i].TxnId);
                }
            })
            .PrintToConsole($"Remove() {COUNT:N0} entries")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");
        }

        [BruteForceBenchmark("RP-05", "Measure enumeration performance for SegmentGhostTransactionnalMap", "Index")]
        public void Benchmark_Enumeration()
        {
            using var store = new PinnedSegmentStore(COUNT);
            var map = new SegmentGhostTransactionnalMap(store);

            // Insert all data
            for (int i = 0; i < COUNT; i++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)i, XorShift64.Next());

                var headerPtr = store.GetHeaderPointer(i);
                headerPtr->Id = id;
                headerPtr->TxnId = i;

                map.Set(store.CreateReference(i), headerPtr);
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");
            int enumCount = 0;

            // Measure enumeration
            RunMonitoredAction(() =>
            {
                var enumerator = map.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    enumCount++;
                }
            })
            .PrintToConsole($"Enumerate {COUNT:N0} entries (found {enumCount:N0})")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-06", "Measure deduplicated enumeration performance", "Index")]
        public void Benchmark_DeduplicatedEnumeration()
        {
            const int ENTITY_COUNT = 10_000_000;
            const int VERSIONS_PER_ENTITY = 10;
            const int MULTI_VERSIONS_PERCENT = 5;
            int totalEntries = ENTITY_COUNT * VERSIONS_PER_ENTITY;

            using var store = new PinnedSegmentStore(totalEntries);
            var map = new SegmentGhostTransactionnalMap(store);

            // Insert all data with multiple versions per entity
            var rnd = new Random(12345);
            int entryIndex = 0;
            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    map.Set(store.CreateReference(entryIndex), headerPtr);
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");

            for (var thCount = 1; thCount <= Environment.ProcessorCount; thCount++)
            {
                if (thCount != 1 && thCount % 2 != 0)
                    continue; // Skip odd counts except 1
                int enumCount = 0;
                // Measure deduplicated enumeration
                RunParallelAction(thCount, (th) =>
                {
                    var enumerator = map.GetDeduplicatedEnumerator(105);
                    while (enumerator.MoveNext())
                    {
                        Interlocked.Increment(ref enumCount);
                    }
                })
                .PrintToConsole($"Thread count = {thCount} / Deduplicated enumerate {entryIndex:N0} entries -> {enumCount:N0} unique entities")
                .PrintDelayPerOp(enumCount > 0 ? enumCount * thCount : 1)
                .PrintSpace();
            }
        }
    }
}
