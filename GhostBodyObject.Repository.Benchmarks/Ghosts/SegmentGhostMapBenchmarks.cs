using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Structs;
using Spectre.Console;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using SmallMapType = SegmentGhostMap<GhostBodyObject.Repository.Benchmarks.Ghosts.SegmentGhostMapBenchmarks.SmallPinnedSegmentStore>;
using LargeMapType = SegmentGhostMap<GhostBodyObject.Repository.Benchmarks.Ghosts.SegmentGhostMapBenchmarks.LargePinnedSegmentStore>;

namespace GhostBodyObject.Repository.Benchmarks.Ghosts
{
    public unsafe class SegmentGhostMapBenchmarks : BenchmarkBase
    {
        private const int ENTITY_COUNT = 10_000_000;
        private const int VERSIONS_PER_ENTITY = 5;
        private const int MULTI_VERSIONS_PERCENT = 5;
        private const int MAX_ENTRIES = ENTITY_COUNT * VERSIONS_PER_ENTITY;

        // ---------------------------------------------------------
        // REALISTIC MEMORY BLOCK STRUCTURES FOR BENCHMARKS
        // In real life, each GhostHeader is hosted in a larger memory
        // block. These structures simulate small (64 bytes) and large
        // (640 bytes) body sizes to create realistic cache miss patterns.
        // ---------------------------------------------------------
        public const int SMALL_BLOCK_SIZE = 64;
        public const int LARGE_BLOCK_SIZE = 640;

        [StructLayout(LayoutKind.Explicit, Size = SMALL_BLOCK_SIZE)]
        public struct SmallGhostBlock
        {
            /// <summary>
            /// The GhostHeader at the beginning of the block (40 bytes).
            /// </summary>
            [FieldOffset(0)]
            public GhostHeader Header;

            /// <summary>
            /// Padding to simulate small body data (64 bytes total).
            /// </summary>
            [FieldOffset(40)]
            private fixed byte _bodyPadding[SMALL_BLOCK_SIZE - GhostHeader.SIZE];
        }

        [StructLayout(LayoutKind.Explicit, Size = LARGE_BLOCK_SIZE)]
        public struct LargeGhostBlock
        {
            /// <summary>
            /// The GhostHeader at the beginning of the block (40 bytes).
            /// </summary>
            [FieldOffset(0)]
            public GhostHeader Header;

            /// <summary>
            /// Padding to simulate large body data (640 bytes total).
            /// </summary>
            [FieldOffset(40)]
            private fixed byte _bodyPadding[LARGE_BLOCK_SIZE - GhostHeader.SIZE];
        }

        // ---------------------------------------------------------
        // SMALL PINNED STORE IMPLEMENTATION (64 bytes per block)
        // ---------------------------------------------------------
        public class SmallPinnedSegmentStore : ISegmentStore, IDisposable
        {
            private SmallGhostBlock[] _blocks;
            private GCHandle _handle;
            private SmallGhostBlock* _basePointer;
            private int _usageCount = 0;

            public SmallPinnedSegmentStore(int capacity)
            {
                _blocks = new SmallGhostBlock[capacity];
                _handle = GCHandle.Alloc(_blocks, GCHandleType.Pinned);
                _basePointer = (SmallGhostBlock*)_handle.AddrOfPinnedObject();
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                    _handle.Free();
            }

            public GhostHeader* GetHeaderPointer(int index) => &(_basePointer + index)->Header;

            public SegmentReference CreateReference(int index) => new SegmentReference
            {
                SegmentId = 0,
                Offset = (uint)(index * SMALL_BLOCK_SIZE)
            };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
                => (GhostHeader*)((byte*)_basePointer + reference.Offset);


            public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
            {
                throw new NotImplementedException();
            }


            public void IncrementSegmentHolderUsage(uint segmentId)
            {
                Interlocked.Increment(ref _usageCount);
            }

            public void DecrementSegmentHolderUsage(uint segmentId)
            {
                Interlocked.Decrement(ref _usageCount);
            }

            public long TotalMemoryBytes => (long)_blocks.Length * SMALL_BLOCK_SIZE;
        }

        // ---------------------------------------------------------
        // LARGE PINNED STORE IMPLEMENTATION (640 bytes per block)
        // ---------------------------------------------------------
        public class LargePinnedSegmentStore : ISegmentStore, IDisposable
        {
            private LargeGhostBlock[] _blocks;
            private GCHandle _handle;
            private LargeGhostBlock* _basePointer;
            private int _usageCount = 0;

            public LargePinnedSegmentStore(int capacity)
            {
                _blocks = new LargeGhostBlock[capacity];
                _handle = GCHandle.Alloc(_blocks, GCHandleType.Pinned);
                _basePointer = (LargeGhostBlock*)_handle.AddrOfPinnedObject();
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                    _handle.Free();
            }

            public GhostHeader* GetHeaderPointer(int index) => &(_basePointer + index)->Header;

            public SegmentReference CreateReference(int index) => new SegmentReference
            {
                SegmentId = 0,
                Offset = (uint)(index * LARGE_BLOCK_SIZE)
            };

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
                => (GhostHeader*)((byte*)_basePointer + reference.Offset);
                
            public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
            {
                throw new NotImplementedException();
            }

            public void IncrementSegmentHolderUsage(uint segmentId)
            {
                Interlocked.Increment(ref _usageCount);
            }

            public void DecrementSegmentHolderUsage(uint segmentId)
            {
                Interlocked.Decrement(ref _usageCount);
            }

            public long TotalMemoryBytes => (long)_blocks.Length * LARGE_BLOCK_SIZE;
        }

        // ---------------------------------------------------------
        // PREPARED DATA STRUCTURE
        // ---------------------------------------------------------
        private struct PreparedEntry
        {
            public SegmentReference Reference;
            public GhostHeader* HeaderPointer;
        }

        [BruteForceBenchmark("RP-01", "Measure Set() insertion performance (Small Body 64B)", "Index")]
        public void Benchmark_Set_Insertion_SmallBody()
        {
            using var store = new SmallPinnedSegmentStore(MAX_ENTRIES);
            var map = new SmallMapType(store);

            // Prepare all data outside the measured loop
            var entries = new PreparedEntry[MAX_ENTRIES];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    entries[entryIndex] = new PreparedEntry
                    {
                        Reference = store.CreateReference(entryIndex),
                        HeaderPointer = headerPtr
                    };
                    entryIndex++;
                }
            }

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < entryIndex; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {entryIndex:N0} entries (Small Body {SMALL_BLOCK_SIZE}B)")
            .PrintDelayPerOp(entryIndex)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");
        }

        [BruteForceBenchmark("RP-02", "Measure Set() insertion performance (Large Body 640B)", "Index")]
        public void Benchmark_Set_Insertion_LargeBody()
        {
            using var store = new LargePinnedSegmentStore(MAX_ENTRIES);
            var map = new LargeMapType(store);

            // Prepare all data outside the measured loop
            var entries = new PreparedEntry[MAX_ENTRIES];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());
                
                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    entries[entryIndex] = new PreparedEntry
                    {
                        Reference = store.CreateReference(entryIndex),
                        HeaderPointer = headerPtr
                    };
                    entryIndex++;
                }
            }

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < entryIndex; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {entryIndex:N0} entries (Large Body {LARGE_BLOCK_SIZE}B)")
            .PrintDelayPerOp(entryIndex)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");
        }

        [BruteForceBenchmark("RP-03", "Measure Get() lookup performance (Small Body 64B)", "Index")]
        public void Benchmark_Get_Lookup_SmallBody()
        {
            using var store = new SmallPinnedSegmentStore(MAX_ENTRIES);
            var map = new SmallMapType(store);

            // Prepare and insert all data
            var ids = new GhostId[ENTITY_COUNT];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());
                ids[e] = id;

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    map.Set(store.CreateReference(entryIndex), headerPtr);
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            long maxTxnId = 200;

            // Measure only the Get() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < ENTITY_COUNT; i++)
                {
                    map.Get(ids[i], maxTxnId, out _);
                }
            })
            .PrintToConsole($"Get() {ENTITY_COUNT:N0} lookups (Small Body {SMALL_BLOCK_SIZE}B)")
            .PrintDelayPerOp(ENTITY_COUNT)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-04", "Measure Get() lookup performance (Large Body 640B)", "Index")]
        public void Benchmark_Get_Lookup_LargeBody()
        {
            using var store = new LargePinnedSegmentStore(MAX_ENTRIES);
            var map = new LargeMapType(store);

            // Prepare and insert all data
            var ids = new GhostId[ENTITY_COUNT];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());
                ids[e] = id;

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    map.Set(store.CreateReference(entryIndex), headerPtr);
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            long maxTxnId = 200;

            // Measure only the Get() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < ENTITY_COUNT; i++)
                {
                    map.Get(ids[i], maxTxnId, out _);
                }
            })
            .PrintToConsole($"Get() {ENTITY_COUNT:N0} lookups (Large Body {LARGE_BLOCK_SIZE}B)")
            .PrintDelayPerOp(ENTITY_COUNT)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-05", "Measure Set() with multiple versions (Small Body 64B)", "Index")]
        public void Benchmark_Set_MultipleVersions_SmallBody()
        {
            const int ENTITY_COUNT = 1_000_000;
            int totalEntries = ENTITY_COUNT * VERSIONS_PER_ENTITY;

            using var store = new SmallPinnedSegmentStore(totalEntries);
            var map = new SmallMapType(store);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < totalEntries; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {totalEntries:N0} entries ({ENTITY_COUNT:N0} entities x {VERSIONS_PER_ENTITY} versions) (Small Body {SMALL_BLOCK_SIZE}B)")
            .PrintDelayPerOp(totalEntries)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-06", "Measure Set() with multiple versions (Large Body 640B)", "Index")]
        public void Benchmark_Set_MultipleVersions_LargeBody()
        {
            const int ENTITY_COUNT = 1_000_000;
            int totalEntries = ENTITY_COUNT * VERSIONS_PER_ENTITY;

            using var store = new LargePinnedSegmentStore(totalEntries);
            var map = new LargeMapType(store);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            // Measure only the Set() calls
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < totalEntries; i++)
                {
                    map.Set(entries[i].Reference, entries[i].HeaderPointer);
                }
            })
            .PrintToConsole($"Set() {totalEntries:N0} entries ({ENTITY_COUNT:N0} entities x {VERSIONS_PER_ENTITY} versions) (Large Body {LARGE_BLOCK_SIZE}B)")
            .PrintDelayPerOp(totalEntries)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-07", "Measure Remove() performance (Small Body 64B)", "Index")]
        public void Benchmark_Remove_SmallBody()
        {
            using var store = new SmallPinnedSegmentStore(MAX_ENTRIES);
            var map = new SmallMapType(store);

            var ids = new GhostId[ENTITY_COUNT];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());
                ids[e] = id;

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    map.Set(store.CreateReference(entryIndex), headerPtr);
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            long removeTxnId = 200;

            RunMonitoredAction(() =>
            {
                for (int i = 0; i < ENTITY_COUNT; i++)
                {
                    map.Remove(ids[i], removeTxnId);
                }
            })
            .PrintToConsole($"Remove() {ENTITY_COUNT:N0} entities (Small Body {SMALL_BLOCK_SIZE}B)")
            .PrintDelayPerOp(ENTITY_COUNT)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");
        }

        [BruteForceBenchmark("RP-08", "Measure Remove() performance (Large Body 640B)", "Index")]
        public void Benchmark_Remove_LargeBody()
        {
            using var store = new LargePinnedSegmentStore(MAX_ENTRIES);
            var map = new LargeMapType(store);

            var ids = new GhostId[ENTITY_COUNT];
            int entryIndex = 0;
            var rnd = new Random(12345);

            for (int e = 0; e < ENTITY_COUNT; e++)
            {
                var id = new GhostId(GhostIdKind.Entity, 50, (ulong)e, XorShift64.Next());
                ids[e] = id;

                for (int v = 0; v < (rnd.Next(100) < MULTI_VERSIONS_PERCENT ? VERSIONS_PER_ENTITY : 1); v++)
                {
                    var headerPtr = store.GetHeaderPointer(entryIndex);
                    headerPtr->Id = id;
                    headerPtr->TxnId = 100 + v;

                    map.Set(store.CreateReference(entryIndex), headerPtr);
                    entryIndex++;
                }
            }

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            long removeTxnId = 200;

            RunMonitoredAction(() =>
            {
                for (int i = 0; i < ENTITY_COUNT; i++)
                {
                    map.Remove(ids[i], removeTxnId);
                }
            })
            .PrintToConsole($"Remove() {ENTITY_COUNT:N0} entities (Large Body {LARGE_BLOCK_SIZE}B)")
            .PrintDelayPerOp(ENTITY_COUNT)
            .PrintSpace();

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo.");
        }

        [BruteForceBenchmark("RP-09", "Measure enumeration performance (Small Body 64B)", "Index")]
        public void Benchmark_Enumeration_SmallBody()
        {
            using var store = new SmallPinnedSegmentStore(MAX_ENTRIES);
            var map = new SmallMapType(store);

            int entryIndex = 0;
            var rnd = new Random(12345);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");
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
            .PrintToConsole($"Enumerate {entryIndex:N0} entries (found {enumCount:N0}) (Small Body {SMALL_BLOCK_SIZE}B)")
            .PrintDelayPerOp(entryIndex)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-10", "Measure enumeration performance (Large Body 640B)", "Index")]
        public void Benchmark_Enumeration_LargeBody()
        {
            using var store = new LargePinnedSegmentStore(MAX_ENTRIES);
            var map = new LargeMapType(store);

            int entryIndex = 0;
            var rnd = new Random(12345);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");
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
            .PrintToConsole($"Enumerate {entryIndex:N0} entries (found {enumCount:N0}) (Large Body {LARGE_BLOCK_SIZE}B)")
            .PrintDelayPerOp(entryIndex)
            .PrintSpace();
        }

        [BruteForceBenchmark("RP-11", "Measure deduplicated enumeration (Small Body 64B)", "Index")]
        public void Benchmark_DeduplicatedEnumeration_SmallBody()
        {
            using var store = new SmallPinnedSegmentStore(MAX_ENTRIES);
            var map = new SmallMapType(store);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            var enumerator = map.GetDeduplicatedEnumerator(105);
            while (enumerator.MoveNext())
            {
            }

            for (var thCount = 1; thCount <= Environment.ProcessorCount; thCount++)
            {
                if (thCount != 1 && thCount % 2 != 0)
                    continue;
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
                .PrintToConsole($"Thread count = {thCount} / Deduplicated enumerate {entryIndex:N0} entries -> {enumCount:N0} unique entities (Small Body {SMALL_BLOCK_SIZE}B)")
                .PrintDelayPerOp(enumCount > 0 ? enumCount * thCount : 1)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("RP-12", "Measure deduplicated enumeration (Large Body 640B)", "Index")]
        public void Benchmark_DeduplicatedEnumeration_LargeBody()
        {
            using var store = new LargePinnedSegmentStore(MAX_ENTRIES);
            var map = new LargeMapType(store);

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

            AnsiConsole.WriteLine($"        -> Map Capacity = {map.Capacity:N0} for {((map.Capacity * 8) / 1024 / 1024):N0} Mo. Store = {store.TotalMemoryBytes / 1024 / 1024:N0} Mo.");

            var enumerator = map.GetDeduplicatedEnumerator(105);
            while (enumerator.MoveNext())
            {
            }
            for (var thCount = 1; thCount <= Environment.ProcessorCount; thCount++)
            {
                if (thCount != 1 && thCount % 2 != 0)
                    continue;
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
                .PrintToConsole($"Thread count = {thCount} / Deduplicated enumerate {entryIndex:N0} entries -> {enumCount:N0} unique entities (Large Body {LARGE_BLOCK_SIZE}B)")
                .PrintDelayPerOp(enumCount > 0 ? enumCount * thCount : 1)
                .PrintSpace();
            }
        }
    }
}
