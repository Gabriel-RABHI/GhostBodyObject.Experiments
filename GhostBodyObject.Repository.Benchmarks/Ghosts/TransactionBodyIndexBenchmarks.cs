using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Utilities;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Transaction.Index;
using Spectre.Console;

namespace GhostBodyObject.Repository.Benchmarks.Ghosts
{
    public unsafe class TransactionBodyIndexBenchmarks : BenchmarkBase
    {
        private const int COUNT = 1_000_000;
        private const int SMALL_COUNT = 100_000;

        [BruteForceBenchmark("TBI-01", "Measure Set() insertion performance for TransactionBodyIndex", "Index")]
        public void Benchmark_Set_Insertion()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Prepare all users outside the measured loop
                var users = new BloggerUser[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                }

                // Measure only the Set() calls
                RunMonitoredAction(() =>
                {
                    for (int i = 0; i < COUNT; i++)
                    {
                        index.Set(users[i]);
                    }
                })
                .PrintToConsole($"Set() {COUNT:N0} unique entries")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");
            }
        }

        [BruteForceBenchmark("TBI-02", "Measure GetRef() lookup performance for TransactionBodyIndex", "Index")]
        public void Benchmark_GetRef_Lookup()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Prepare and insert all users
                var users = new BloggerUser[COUNT];
                var ids = new GhostId[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");

                // Measure only the GetRef() calls
                RunMonitoredAction(() =>
                {
                    for (int i = 0; i < COUNT; i++)
                    {
                        index.GetRef(ids[i], out _);
                    }
                })
                .PrintToConsole($"GetRef() {COUNT:N0} lookups")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("TBI-03", "Measure Remove() performance for TransactionBodyIndex", "Index")]
        public void Benchmark_Remove()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Prepare and insert all users
                var users = new BloggerUser[COUNT];
                var ids = new GhostId[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                    ids[i] = users[i].Header->Id;
                    index.Set(users[i]);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity before remove = {index.Capacity:N0}, Count = {index.Count:N0}");

                // Measure only the Remove() calls
                RunMonitoredAction(() =>
                {
                    for (int i = 0; i < COUNT; i++)
                    {
                        index.Remove(ids[i]);
                    }
                })
                .PrintToConsole($"Remove() {COUNT:N0} entries")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                AnsiConsole.WriteLine($"        -> Index Capacity after remove = {index.Capacity:N0}, Count = {index.Count:N0}");
            }
        }

        [BruteForceBenchmark("TBI-04", "Measure enumeration performance for TransactionBodyIndex", "Index")]
        public void Benchmark_Enumeration()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Insert all users
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    index.Set(user);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");

                int enumCount = 0;

                // Measure enumeration using struct enumerator
                RunMonitoredAction(() =>
                {
                    var enumerator = index.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        enumCount++;
                    }
                })
                .PrintToConsole($"Enumerate {COUNT:N0} entries (found {enumCount:N0})")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("TBI-05", "Measure raw array enumeration performance for TransactionBodyIndex", "Index")]
        public void Benchmark_RawArrayEnumeration()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Insert all users
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    index.Set(user);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");

                int enumCount = 0;

                // Measure enumeration using raw array access
                RunMonitoredAction(() =>
                {
                    var entries = index.GetEntriesArray();
                    for (int i = 0; i < entries.Length; i++)
                    {
                        if (entries[i] != null)
                        {
                            enumCount++;
                        }
                    }
                })
                .PrintToConsole($"Raw array enumerate {COUNT:N0} entries (found {enumCount:N0})")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("TBI-06", "Measure Set/Update performance for TransactionBodyIndex", "Index")]
        public void Benchmark_SetUpdate()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Insert initial users
                var users = new BloggerUser[SMALL_COUNT];
                for (int i = 0; i < SMALL_COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                    index.Set(users[i]);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");

                // Measure updating existing entries
                RunMonitoredAction(() =>
                {
                    for (int i = 0; i < SMALL_COUNT; i++)
                    {
                        index.Set(users[i]); // Same entry, triggers update path
                    }
                })
                .PrintToConsole($"Set() update {SMALL_COUNT:N0} existing entries")
                .PrintDelayPerOp(SMALL_COUNT)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("TBI-07", "Measure mixed Set/GetRef/Remove operations for TransactionBodyIndex", "Index")]
        public void Benchmark_MixedOperations()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var index = new TransactionBodyIndex<BloggerUser>();

                // Prepare users
                var users = new BloggerUser[SMALL_COUNT];
                var ids = new GhostId[SMALL_COUNT];
                for (int i = 0; i < SMALL_COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                    ids[i] = users[i].Header->Id;
                }

                int operationCount = SMALL_COUNT * 3;

                // Measure mixed operations: insert, lookup, remove cycle
                RunMonitoredAction(() =>
                {
                    // Insert all
                    for (int i = 0; i < SMALL_COUNT; i++)
                    {
                        index.Set(users[i]);
                    }

                    // Lookup all
                    for (int i = 0; i < SMALL_COUNT; i++)
                    {
                        index.GetRef(ids[i], out _);
                    }

                    // Remove all
                    for (int i = 0; i < SMALL_COUNT; i++)
                    {
                        index.Remove(ids[i]);
                    }
                })
                .PrintToConsole($"Mixed ops: {SMALL_COUNT:N0} Set + {SMALL_COUNT:N0} GetRef + {SMALL_COUNT:N0} Remove")
                .PrintDelayPerOp(operationCount)
                .PrintSpace();
            }
        }

        [BruteForceBenchmark("TBI-08", "Compare Set/GetRef/Remove with Dictionary<GhostId, TBody>", "Index")]
        public void Benchmark_CompareWithDictionary()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                // Prepare users
                var users = new BloggerUser[SMALL_COUNT];
                var ids = new GhostId[SMALL_COUNT];
                for (int i = 0; i < SMALL_COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                    ids[i] = users[i].Header->Id;
                }

                var results = new List<BenchmarkResult>();

                // Benchmark TransactionBodyIndex
                var index = new TransactionBodyIndex<BloggerUser>();
                var indexResult = RunMonitoredAction(() =>
                {
                    for (int i = 0; i < SMALL_COUNT; i++)
                        index.Set(users[i]);
                    for (int i = 0; i < SMALL_COUNT; i++)
                        index.GetRef(ids[i], out _);
                    for (int i = 0; i < SMALL_COUNT; i++)
                        index.Remove(ids[i]);
                });
                results.Add(indexResult.WithLabel("TransactionBodyIndex").WithOperations(SMALL_COUNT * 3));

                // Benchmark Dictionary
                var dict = new Dictionary<GhostId, BloggerUser>();
                var dictResult = RunMonitoredAction(() =>
                {
                    for (int i = 0; i < SMALL_COUNT; i++)
                        dict[ids[i]] = users[i];
                    for (int i = 0; i < SMALL_COUNT; i++)
                        dict.TryGetValue(ids[i], out _);
                    for (int i = 0; i < SMALL_COUNT; i++)
                        dict.Remove(ids[i]);
                });
                results.Add(dictResult.WithLabel("Dictionary<GhostId, TBody>").WithOperations(SMALL_COUNT * 3));

                PrintComparison(
                    $"Set + GetRef + Remove ({SMALL_COUNT:N0} ops each)",
                    "TransactionBodyIndex vs Dictionary<GhostId, TBody>",
                    results);
            }
        }

        [BruteForceBenchmark("TBI-09", "Measure Clear() performance for TransactionBodyIndex", "Index")]
        public void Benchmark_Clear()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                const int iterations = 100;

                // Pre-create index filled with entries
                var index = new TransactionBodyIndex<BloggerUser>();
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    index.Set(user);
                }

                AnsiConsole.WriteLine($"        -> Index Capacity = {index.Capacity:N0}, Count = {index.Count:N0}");

                // Measure Clear() - need to refill between clears
                var users = new BloggerUser[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    users[i] = new BloggerUser();
                    users[i].Active = true;
                }

                RunMonitoredAction(() =>
                {
                    for (int iter = 0; iter < iterations; iter++)
                    {
                        index.Clear();
                        // Refill
                        for (int i = 0; i < COUNT; i++)
                        {
                            index.Set(users[i]);
                        }
                    }
                })
                .PrintToConsole($"Clear() + refill {COUNT:N0} entries x {iterations} iterations")
                .PrintDelayPerOp((long)iterations * COUNT)
                .PrintSpace();
            }
        }
    }
}
