using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Concepts.Performances.Utils;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Repository.Constants;

namespace GhostBodyObject.Concepts.Performances.BloggerApp
{
    public class BloggerAppBenchmarks : BenchmarkBase
    {
        private const long _1M = 1_000_000;

        [BruteForceBenchmark("OBJ-01", "Insert -> Mutate ( +Enumerate ) -> Remove", "Repository")]
        public void InsertMutateRemove()
        {
            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 1M - light
                (SegmentStoreMode.InMemoryVolatileRepository, 100, 10_000, false),
                (SegmentStoreMode.InMemoryVolatileRepository, 1_000, 1_000, false),
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 100, false),
                // -------- 1M - fat
                (SegmentStoreMode.InMemoryVolatileRepository, 100, 10_000, true),
                (SegmentStoreMode.InMemoryVolatileRepository, 1_000, 1_000, true),
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 100, true),

                // -------- 10M - light
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 1_000, false),
                (SegmentStoreMode.InMemoryVolatileRepository, 100_000, 100, false),
                // -------- 10M - fat
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 1_000, true),
                (SegmentStoreMode.InMemoryVolatileRepository, 100_000, 100, true),

                // -------- 10M - light
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 10_000, 1_000, false),
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 100_000, 100, false),
                // -------- 10M - fat
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 10_000, 1_000, true),
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 100_000, 100, true),
                
                // -------- 10M - light
                (SegmentStoreMode.PersistantRepository, 10_000, 1_000, false),
                (SegmentStoreMode.PersistantRepository, 100_000, 100, false),
                // -------- 10M - fat
                (SegmentStoreMode.PersistantRepository, 10_000, 1_000, true),
                (SegmentStoreMode.PersistantRepository, 100_000, 100, true),

                // -------- 100M - light
                (SegmentStoreMode.PersistantRepository, 100_000, 1_000, false),
                (SegmentStoreMode.PersistantRepository, 1_000_000, 100, false),
            };
            var modes = new SegmentStoreMode[] {
                SegmentStoreMode.InMemoryVolatileRepository,
                SegmentStoreMode.InVirtualMemoryVolatileRepository,
                SegmentStoreMode.PersistantRepository
            };
            var enumerations = new (bool, bool)[] {
                (true, true),
                (true, false),
                (false, false)
            };
            foreach (var (concurrentEnumeration, useCursors) in enumerations)
                foreach (var (mode, transactionCount, objectPerTransaction, fatObjects) in transactionCounts)
                {
                    if (mode != SegmentStoreMode.InMemoryVolatileRepository)
                    {
                        using (var dir = new BenchTempDirectory(true))
                        {
                            InsertMutateRemove(mode, transactionCount, objectPerTransaction, fatObjects, concurrentEnumeration, useCursors, dir.DirectoryPath);
                        }
                    } else
                    {
                        InsertMutateRemove(mode, transactionCount, objectPerTransaction, fatObjects, concurrentEnumeration, useCursors, null);
                    }
                    Console.WriteLine("Wait...");
                    for (int i = 0; i < 20; i++)
                    {
                        BenchTempDirectory.GCCollect();
                        Thread.Sleep(250);
                        Console.Write(".");
                    }
                }
        }

        public void InsertMutateRemove(SegmentStoreMode mode, int transactionCount, int objectPerTransaction, bool fatObjects, bool concurrentEnumeration, bool singleWriter, string path)
        {
            var nproc = Math.Max(1, Environment.ProcessorCount / 2);
            var nthreads = Math.Max(1, concurrentEnumeration ? nproc / 2 : nproc);

            var modeString = mode.ToString().Replace("InMemoryVolatileRepository", "Memory").Replace("InVirtualMemoryVolatileRepository", "Virtual").Replace("PersistantRepository", "Persistant");

            WriteComment($"{modeString} - Inserting {(transactionCount * objectPerTransaction) / _1M}M {(fatObjects ? "FAT" : "light")} BloggerUser objects using {(singleWriter ? 1 : nthreads)} threads, {transactionCount} transactions" + (concurrentEnumeration ? $" + {(singleWriter ? "cursor" : "instance")} enumeration." : "."));
            if (!string.IsNullOrEmpty(path))
                Console.WriteLine($"Storage directory : {path}");

            using var repository = new BloggerRepository(mode, path);
            long totalInsertedObjects = 0;
            long totalReadedObjects = 0;
            long totalEnumerations = 0;
            RunParallelAction(nproc, (nth) => {
                if ((singleWriter && nth == 0) || (!singleWriter && nth < nthreads))
                {
                    int baseLoad = objectPerTransaction / nthreads;
                    int remainder = objectPerTransaction % nthreads;
                    int itemsToInsert = baseLoad + (nth < remainder ? 1 : 0);

                    if (singleWriter)
                        itemsToInsert = objectPerTransaction;

                    for (int t = 0; t < transactionCount; t++)
                    {
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            if (fatObjects)
                            {
                                for (int i = 0; i < itemsToInsert; i++)
                                {
                                    var user = new BloggerUser() {
                                        FirstName = $"Initial-{t}-{i}",
                                        LastName = $"LastName-{t}-{i}",
                                        Address1 = $"Address1-{t}-{i}",
                                        Address2 = $"Address2-{t}-{i}",
                                        Address3 = $"Address3-{t}-{i}",
                                        City = $"City-{t}-{i}",
                                        CompanyName = $"CompanyName-{t}-{i}",
                                        Country = $"Country-{t}-{i}",
                                        Hobbies = $"Hobbies-{t}-{i}",
                                        Pseudonyme = $"Pseudonyme-{t}-{i}",
                                        Presentation = $"Presentation-{t}-{i}",
                                        ZipCode = $"ZipCode-{t}-{i}",
                                        BirthDate = DateTime.UtcNow,
                                        CustomerCode = i,
                                        Active = true
                                    };
                                }
                            } else
                            {
                                for (int i = 0; i < itemsToInsert; i++)
                                {
                                    var user = new BloggerUser() {
                                        FirstName = $"Initial-{t}-{i}",
                                        Active = true
                                    };
                                }
                            }
                            BloggerContext.Commit();
                        }
                        Interlocked.Add(ref totalInsertedObjects, itemsToInsert);
                    }
                } else
                {
                _enumerate:
                    using (BloggerContext.NewReadContext(repository))
                    {
                        /*
                        var n = 0l;
                        foreach (var user in BloggerCollections.BloggerUsers.Cursor)
                            if (user.Active)
                                n++;
                        */
                        var n = BloggerCollections.BloggerUsers.Cursor.Count(u => u.Active);

                        Interlocked.Add(ref totalReadedObjects, n);
                        Interlocked.Increment(ref totalEnumerations);
                        if (n < transactionCount * objectPerTransaction)
                            goto _enumerate;
                    }
                }
            })
            .PrintToConsole("Results")
            .PrintDelayPerOp(totalInsertedObjects, "Add objects")
            .PrintDelayPerOp(transactionCount, "Transactions")
            .PrintDelayPerOp(totalReadedObjects, "Retreived objects")
            .PrintSpace();

            Console.WriteLine($"Enumerations count : {totalEnumerations}");
        }
    }
}
