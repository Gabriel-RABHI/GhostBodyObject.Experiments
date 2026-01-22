using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Concepts.Performances.Utils;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace GhostBodyObject.Concepts.Performances.BloggerApp
{
    public class BloggerAppBenchmarks : BenchmarkBase
    {
        private const long _1M = 1_000_000;

        [BruteForceBenchmark("OBJ-01", "In Memory (LOHC) - 10M - no enumerations", "Repository")]
        public void InsertMutateRemove_1()
        {
            WriteSpace();
            WriteDetail($"This benchmark show that :");
            WriteDetail($" - The in memory store is fast : it makes it a new C# programming model.");
            WriteDetail($" - The ghost size have no real impact on performances : it scale linearly with object size.");
            WriteSpace();

            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 10M - light
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 1_000, false),
                (SegmentStoreMode.InMemoryVolatileRepository, 100_000, 100, false),
                // -------- 10M - fat
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 1_000, true),
                (SegmentStoreMode.InMemoryVolatileRepository, 100_000, 100, true),
            };
            var enumerations = new (bool, bool)[] {
                (false, false)
            };
            Excute(transactionCounts, enumerations);
        }

        [BruteForceBenchmark("OBJ-02", "In Memory (LOHC) - 1M + concurrent MVCC enumerations", "Repository")]
        public void InsertMutateRemove_2()
        {
            WriteSpace();
            WriteDetail($"This benchmark show that :");
            WriteDetail($" - The ghost store is gracefully managing Epochs to provide MVCC while aggressive enumerations are performed by a set of threads. The concurrency performance are the same for Virtual Memory or Perstent repositories.");
            WriteDetail($" - The impact of creating one body per enumerator iteration. The 'cursor' mode is a cornerstone of performances.");
            WriteSpace();
            WriteDetail($"Concurrent enumerations are MVCC based : the 'context' see the object collections as snapshot of the entire repository.");
            WriteDetail($"The basic principle of GBO is to provide 'zero copy' and 'zero allocation' object and data management.");
            WriteSpace();
            WriteDetail($"For this reason, there is two enumeration modes :");
            WriteDetail($" - The 'cursor' mode is using a single Body instance to access to each recorded Ghost (a entity property value storage).");
            WriteDetail($" - The 'instance' mode create a new Body instance to access to each recorded Ghost.");
            WriteSpace();
            WriteDetail($"The cursor mode is the one to use everywhere, especially in Linq query for filtering. The instance mode is dedicated to situation where the code retains the bodies (add to a list or a variable).");
            WriteDetail($"A specific .Where() method permit to combine both : when the predicate is true, the Body is dettached and a new cursor is created.");
            WriteSpace();
            WriteDetail($"A another aspect not show here is that all collections are shareded, and support paralell filtering : so, the enumeration rate here can be the one of a single Linq query (!).");
            WriteSpace();

            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 1M - light
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 100, false),
                // -------- 1M - fat
                (SegmentStoreMode.InMemoryVolatileRepository, 10_000, 100, true),
            };
            var enumerations = new (bool, bool)[] {
                (true, true),
                (true, false)
            };
            Excute(transactionCounts, enumerations);
        }

        [BruteForceBenchmark("OBJ-03", "In Virtual Memory (MMF) - 10M", "Repository")]
        public void InsertMutateRemove_3()
        {
            WriteSpace();
            WriteDetail($"This benchmark insert [red]10Go of FAT[/] objects. This mode is an [red]'out of heap'[/] mode. This is a completly new, memory optimized .Net object mode.");
            WriteSpace();
            WriteDetail($"This benchmark (that insert [red]10Go of FAT[/] objects) show that :");
            WriteDetail($" - The use of MMF virtual memory based Segments has low impact on performances.");
            WriteDetail($" - The memory footprint of the process still extremly low.");
            WriteDetail($" - The operating system is gracefully streaming data from the disk to lower memory footprint.");
            WriteSpace();
            WriteDetail($"The enumeration impact is the same as pure In Memory OBJ-02 benchmark.");
            WriteSpace();

            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 10M - light
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 10_000, 1_000, true),
                (SegmentStoreMode.InVirtualMemoryVolatileRepository, 100_000, 100, true),
            };
            var enumerations = new (bool, bool)[] {
                (false, false),
                (true, true),
            };
            Excute(transactionCounts, enumerations);
        }

        [BruteForceBenchmark("OBJ-04", "Persistant (ACID MMF) - 10M", "Repository")]
        public void InsertMutateRemove_4()
        {
            WriteSpace();
            WriteDetail($"This repository mode is ACID (it insert [red]10Go of FAT[/] objects) : each time a transaction is commited, the data are flushed to disk, meeting the durability needed by mission critical applications. A Hash based checksum permit the cancellation of incomplete, interrupted transactions data.");
            WriteSpace();
            WriteDetail($"This benchmark show that :");
            WriteDetail($" - The forced Flush of the memory pages to disk do not slow down significantly the system.");
            WriteDetail($" - The memory footprint of the process still extremly low.");
            WriteSpace();
            WriteDetail($"The enumeration impact is the same as pure In Memory OBJ-02 benchmark.");
            WriteSpace();

            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 10M - light
                (SegmentStoreMode.PersistantRepository, 100_000, 100, true),
            };
            var enumerations = new (bool, bool)[] {
                (false, false),
                (true, true),
            };
            Excute(transactionCounts, enumerations);
        }

        [BruteForceBenchmark("OBJ-04", "Persistant (ACID MMF) - 100M (100 000 000) large collection - no enumerations", "Repository")]
        public void InsertMutateRemove_5()
        {
            WriteSpace();
            WriteDetail($"This benchmark show that :");
            WriteDetail($" - The memory usage of the Ghost repository index is really controlled (1.5 GB).");
            WriteDetail($" - The Segment size variation on disk.");
            WriteSpace();
            WriteDetail($"The enumeration impact is the same as pure In Memory OBJ-02 benchmark.");
            WriteSpace();

            var transactionCounts = new (SegmentStoreMode, int, int, bool)[] {
                // -------- 100M - light
                (SegmentStoreMode.PersistantRepository, 100_000, 1000, false),
            };
            var enumerations = new (bool, bool)[] {
                (false, false)
            };
            Excute(transactionCounts, enumerations);
        }

        public void Excute((SegmentStoreMode, int, int, bool)[] transactionCounts, (bool, bool)[] enumerations)
        {
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
                    Console.Write("    Wait few GC Collet...");
                    for (int i = 0; i < 30; i++)
                    {
                        BenchTempDirectory.GCCollect();
                        Thread.Sleep(250);
                        Console.Write(".");
                    }
                    Console.Write(" done.");
                    Console.WriteLine();
                }
        }

        public void InsertMutateRemove(SegmentStoreMode mode, int transactionCount, int objectPerTransaction, bool fatObjects, bool concurrentEnumeration, bool useCursor, string path)
        {
            var nproc = Math.Max(2, Environment.ProcessorCount / 2);
            var nthreads = concurrentEnumeration ? nproc : 1;

            var modeString = mode.ToString().Replace("InMemoryVolatileRepository", "Memory").Replace("InVirtualMemoryVolatileRepository", "Virtual Mem.").Replace("PersistantRepository", "ACID Persistancy");

            WriteSpace();
            WriteStep($"New Run {modeString}");
            WriteComment($"Insert {(transactionCount * objectPerTransaction) / _1M}M {(fatObjects ? "[Cyan]FAT[/]" : "light")} objs, {transactionCount} txn * {objectPerTransaction} objs" + (concurrentEnumeration ? $" + {nthreads - 1} threads in {(useCursor ? "[green]cursor[/]" : "[red]instance[/]")} MVCC enumerations." : "."));
            if (!string.IsNullOrEmpty(path))
                WriteDetail($"Storage directory : {path}");

            if (concurrentEnumeration && !useCursor)
            {
                WriteSpace();
                WriteDetail($"The 'instance' mode mean the collection enumerator create one Body object per stored Ghost. It will [red]dramatically slow down[/] the system because each Body must be collected by the GC.");
                WriteSpace();
            }

            using var repository = new BloggerRepository(mode, path);
            long totalInsertedObjects = 0;
            long totalReadedObjects = 0;
            long totalEnumerations = 0;
            RunParallelAction(nthreads, (nth) => {
                if (nth == 0)
                {
                    for (int t = 0; t < transactionCount; t++)
                    {
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            if (fatObjects)
                            {
                                for (int i = 0; i < objectPerTransaction; i++)
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
                                for (int i = 0; i < objectPerTransaction; i++)
                                {
                                    var user = new BloggerUser() {
                                        FirstName = $"Initial-{t}-{i}",
                                        Active = true
                                    };
                                }
                            }
                            BloggerContext.Commit();
                        }
                        Interlocked.Add(ref totalInsertedObjects, objectPerTransaction);
                    }
                } else
                {
                _enumerate:
                    using (BloggerContext.NewReadContext(repository))
                    {
                        var n = 0;
                        if (useCursor)
                            n = BloggerCollections.BloggerUsers.Cursor.Count(u => u.Active);
                        else
                            n = BloggerCollections.BloggerUsers.Instances.Count(u => u.Active);

                        Interlocked.Add(ref totalReadedObjects, n);
                        Interlocked.Increment(ref totalEnumerations);
                        if (n < transactionCount * objectPerTransaction)
                            goto _enumerate;
                    }
                }
            })
            .PrintToConsole("Results")
            .PrintDelayPerOp(totalInsertedObjects, "Add objects", false)
            .PrintDelayPerOp(transactionCount, "Transactions", false)
            .PrintDelayPerOp(totalReadedObjects, "Retreived objects", false)
            .PrintDelayPerOp(totalEnumerations, "Enumerations", false)
            .PrintSpace();
        }






        [BruteForceBenchmark("OBJ-05", "10M collection concurrent enumerations and point retreives", "Repository")]
        public void ConcurrentEnumerations()
        {
            WriteSpace();
            WriteDetail($"This benchmark show the enumeration performances :");
            WriteDetail($" - The enumeration capability scale linearly with thread count.");
            WriteDetail($" - Same for point retreives.");
            WriteSpace();

            using (var dir = new BenchTempDirectory(true))
            {
                using var repository = new BloggerRepository(SegmentStoreMode.PersistantRepository, dir.DirectoryPath);
                List<GhostId> ids = new();
                for (int t = 0; t < 10_000; t++)
                {
                    using (BloggerContext.NewWriteContext(repository))
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            var user = new BloggerUser() {
                                FirstName = $"Initial-{t}-{i}",
                                Active = true
                            };
                            ids.Add(user.Id);
                        }
                        BloggerContext.Commit();
                    }
                }

                if (true)
                {
                    var nproc = Math.Max(2, Environment.ProcessorCount / 2);
                    var thcount = 1;
                    long totalReadedObjects = 0;
                    long totalEnumerations = 0;
                    long totalRetreives = 0;
                    while (thcount <= nproc)
                    {
                        WriteStep($"New Run with {thcount} threads enumerating in [green]cursor[/] mode.");
                        RunParallelAction(thcount, (nth) => {
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            while (sw.ElapsedMilliseconds < 5 * 1000)
                                using (BloggerContext.NewReadContext(repository))
                                {
                                    var n = BloggerCollections.BloggerUsers.Cursor.Count(u => u.Active);

                                    Interlocked.Add(ref totalReadedObjects, n);
                                    Interlocked.Increment(ref totalEnumerations);
                                }
                        })
                        .PrintToConsole("Enumeration Results")
                        .PrintDelayPerOp(totalReadedObjects, "Enumerated objects", false)
                        .PrintDelayPerOp(totalEnumerations, "Enumerations", false)
                        .PrintSpace();

                        RunParallelAction(thcount, (nth) => {
                            var rnd = new Random(nth * 9973);
                            var sw = System.Diagnostics.Stopwatch.StartNew();
                            var n = 0;
                            while (sw.ElapsedMilliseconds < 5 * 1000)
                                using (BloggerContext.NewReadContext(repository))
                                {
                                    for (int i = 0; i < 100; i++)
                                    {
                                        var body = BloggerCollections.BloggerUsers.Retreive(ids[rnd.Next(ids.Count)]);
                                        n++;
                                    }
                                }
                            Interlocked.Add(ref totalRetreives, n);
                        })
                        .PrintToConsole("Point Retreive Results")
                        .PrintDelayPerOp(totalRetreives, "Retreived objects", true)
                        .PrintSpace();
                        thcount *= 2;
                    }
                }

                if (true)
                {
                    var nproc = Math.Max(2, Environment.ProcessorCount / 2);
                    var thcount = 1;
                    long totalReadedObjects = 0;
                    long totalEnumerations = 0;
                    long totalRetreives = 0;
                    while (thcount <= nproc)
                    {
                        WriteStep($"New Run with {thcount} threads performing [red]instance[/] retreive in single context.");
                        RunParallelAction(thcount, (nth) => {
                            using (BloggerContext.NewReadContext(repository))
                            {
                                var sw = System.Diagnostics.Stopwatch.StartNew();
                                var rnd = new Random(nth * 9973);
                                var n = 0;
                                while (sw.ElapsedMilliseconds < 5 * 1000)
                                {
                                    for (int i = 0; i < 100; i++)
                                    {
                                        var body = BloggerCollections.BloggerUsers.Retreive(ids[rnd.Next(ids.Count)]);
                                        n++;
                                    }
                                }
                                Interlocked.Add(ref totalRetreives, n);
                            }
                        })
                        .PrintToConsole("Enumeration and Point Retreive Results")
                        .PrintDelayPerOp(totalRetreives, "Retreived objects", true)
                        .PrintSpace();
                        thcount *= 2;
                    }
                }
            }
        }

        [BruteForceBenchmark("OBJ-06", "1000 objects update / segment release", "Repository")]
        public void SegmentReleasing()
        {
            WriteSpace();
            WriteDetail($"This benchmark show a [red]critical safety feature[/] principle : the effective GC collection of unused segments, and the locking of Segments that are used by residual Bodies.");
            WriteDetail($"When a Ghost is allocated in the memory zone if a Segment, the direct memory pointer must be secure : the Ghost containner must preserve the memory validity.");
            WriteDetail($"In this benchmark, a limited set of 1000 objects are updated : this makes the older version deprecated, and then the Segment not used anymore.");
            WriteDetail($"Few Bodies are retainned to lock few Segments. When trying to access Bodie's properties for wich context (transaction) is closed, the context validity check throw an exception.");
            WriteDetail($"This exeption is linked to lifecycle of Bodies : it is a rule imposed to developpers.");
            WriteSpace();

            using (var dir = new BenchTempDirectory(true))
            {
                long gen = 0;
                using var repository = new BloggerRepository(SegmentStoreMode.PersistantRepository, dir.DirectoryPath);
                using (BloggerContext.NewWriteContext(repository))
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        var user = new BloggerUser() {
                            FirstName = $"Initial-{i}",
                            LastName = $"LastName-{i}",
                            Address1 = $"Address1-{i}",
                            Address2 = $"Address2-{i}",
                            Address3 = $"Address3-{i}",
                            City = $"City-{i}",
                            CompanyName = $"CompanyName-{i}",
                            Country = $"Country-{i}",
                            Hobbies = $"Hobbies-{i}",
                            Pseudonyme = $"Pseudonyme-{i}",
                            Presentation = $"Presentation-{i}",
                            ZipCode = $"ZipCode-{i}",
                            BirthDate = DateTime.UtcNow,
                            CustomerCode = i,
                            Active = true
                        };
                    }
                    BloggerContext.Commit();
                }

                var totalMutations = 0l;
                var totalEnumerations = 0l;
                var objects = new List<BloggerUser>();
                RunParallelAction(2, (nth) => {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var updt = System.Diagnostics.Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < 20 * 1000)
                    {
                        if (nth == 0)
                        {
                            using (BloggerContext.NewWriteContext(repository))
                            {
                                BloggerCollections.BloggerUsers.Scan(b => b.FirstName = $"Scanned-{totalMutations++}");
                                BloggerContext.Commit();
                            }
                        } else
                        {
                            using (BloggerContext.NewReadContext(repository))
                            {
                                BloggerCollections.BloggerUsers.ForEach(b => {
                                    totalEnumerations++;
                                    if (updt.ElapsedMilliseconds > 2000)
                                    {
                                        objects.Add(b);
                                        updt = System.Diagnostics.Stopwatch.StartNew();
                                        WriteDetail($"-------- {dir.GetFiles().Count()} files / Mutations = {totalMutations} / Enumerations = {totalEnumerations}");
                                        foreach (var file in dir.GetFiles())
                                            WriteDetail(file);
                                        foreach (var obj in objects)
                                        {
                                            try
                                            {
                                                WriteDetail(obj.FirstName);
                                            } catch (Exception ex)
                                            {
                                                WriteDetail($"      => Context 'protection' logical error : {ex.Message}");
                                            }
                                        }
                                    }
                                });
                            }
                        }
                    }
                })
                .PrintToConsole("Results")
                .PrintDelayPerOp(totalMutations, "Mutations", false)
                .PrintDelayPerOp(totalEnumerations, "Enumerated objects", false)
                .PrintSpace();
            }
        }
    }
}
