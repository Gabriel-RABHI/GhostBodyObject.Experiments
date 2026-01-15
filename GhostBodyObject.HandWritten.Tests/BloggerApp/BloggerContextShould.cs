using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Segment;
using System.Diagnostics;
using System.IO;
using System.Transactions;

namespace GhostBodyObject.HandWritten.Tests.BloggerApp
{
    public class BloggerContextShould
    {
        [Fact]
        public void OpenAndAssignTransactions()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = new BloggerUser();

                user.Active = true;

                var txn = BloggerContext.Transaction;
                Assert.True(user.Transaction == txn);
            }
        }

        [Fact]
        public void ForbidCreationInReadOnlyContexts()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var repository = new BloggerRepository();
                using (BloggerContext.NewReadContext(repository))
                {
                    var user = new BloggerUser();
                }
            });
        }

        [Fact]
        public void ForbidNestedContexts()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var repository = new BloggerRepository();
                using (BloggerContext.NewWriteContext(repository))
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    using (BloggerContext.NewWriteContext(repository))
                    {
                        var user2 = new BloggerUser();
                        user.Active = true;
                    }
                }
            });

        }

        [Fact]
        public void OpenParalellScopes()
        {
            var repository = new BloggerRepository();

            var t1 = Task.Run(() =>
            {
                Thread.Sleep(500);
                using (BloggerContext.NewWriteContext(repository))
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    Thread.Sleep(1000);
                }
            });
            var t2 = Task.Run(() =>
            {
                Thread.Sleep(500);
                using (BloggerContext.NewWriteContext(repository))
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    Thread.Sleep(1000);
                }
            });
            var t3 = Task.Run(() =>
            {
                Thread.Sleep(500);
                using (BloggerContext.NewWriteContext(repository))
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    Thread.Sleep(1000);
                }
            });
            Task.WaitAll(t1, t2, t3);
        }

        [Fact]
        public void ForbidParalellScopesInScope()
        {
            Assert.Throws<AggregateException>(() =>
            {
                var repository = new BloggerRepository();
                using (BloggerContext.NewWriteContext(repository))
                {
                    var t1 = Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            var user = new BloggerUser();
                            user.Active = true;
                            Thread.Sleep(1000);
                        }
                    });
                    var t2 = Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            var user = new BloggerUser();
                            user.Active = true;
                            Thread.Sleep(1000);
                        }
                    });
                    var t3 = Task.Run(() =>
                    {
                        Thread.Sleep(500);
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            var user = new BloggerUser();
                            user.Active = true;
                            Thread.Sleep(1000);
                        }
                    });
                    Task.WaitAll(t1, t2, t3);
                }
            });
        }

        [Fact]
        public void ForbidInTaskMutations()
        {
            var COUNT = 10_000_000;
            Assert.Throws<AggregateException>(() =>
            {
                var repository = new BloggerRepository();
                using (BloggerContext.NewWriteContext(repository))
                {
                    var t1 = Task.Run(() =>
                    {
                        var user = new BloggerUser();
                        for (int i = 0; i < COUNT; i++)
                            user.Active = !user.Active;
                    });
                    var t2 = Task.Run(() =>
                    {
                        var user = new BloggerUser();
                        for (int i = 0; i < COUNT; i++)
                            user.Active = !user.Active;
                    });
                    var t3 = Task.Run(() =>
                    {
                        var user = new BloggerUser();
                        for (int i = 0; i < COUNT; i++)
                            user.Active = !user.Active;
                    });
                    Task.WaitAll(t1, t2, t3);
                }
            });
        }

        [Theory()]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.PersistantRepository)]
        public void AddAndCommitTransactions(SegmentStoreMode mode)
        {
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);
            using (BloggerContext.NewWriteContext(repository))
            {
                var user = new BloggerUser()
                {
                    Active = true,
                    FirstName = "John",
                    LastName = "Doe"
                };
                user = new BloggerUser()
                {
                    Active = true,
                    FirstName = "Ted",
                    LastName = "Smith"
                };
                BloggerContext.Commit(false);
            }
            using (BloggerContext.NewReadContext(repository))
            {
                //Assert.True(BloggerContext.Transaction.BloggerUserCollection.Any());
                BloggerUserCollection.ForEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                });
            }
        }

        [Fact]
        public void ManageBottomTxnIdWhenTxnClosed()
        {
            var repository = new BloggerRepository();
            var sw = Stopwatch.StartNew();
            for (int j = 0; j < 1_000; j++)
            {
                using (BloggerContext.NewWriteContext(repository))
                {
                    var user = new BloggerUser()
                    {
                        Active = true,
                    };
                    BloggerContext.Commit(false);
                }
            }
            Assert.Equal(1000, repository.BottomTransactionId);
        }

#if RELEASE

        [Theory()]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.PersistantRepository)]
        public void AddAndCommitTransactionsLarge(SegmentStoreMode mode)
        {
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);
            var sw = Stopwatch.StartNew();
            long sum = 0;
            for (int j = 0; j < 50_000; j++)
                using (BloggerContext.NewWriteContext(repository))
                {
                    for (int i=0;i< 20; i++)
                    {
                        var user = new BloggerUser()
                        {
                            Active = true,
                            CustomerCode = i + (j % 99)
                        };
                        sum += user.CustomerCode;
                    }
                    BloggerContext.Commit(false);
                }
            Console.WriteLine($"Write and commit users in {sw.ElapsedMilliseconds} ms");
            using (BloggerContext.NewReadContext(repository))
            {
                for (int i = 0; i < 3; i++)
                {
                    long verifySum = 0;
                    var n = 0;
                    sw = Stopwatch.StartNew();
                    BloggerUserCollection.ForEachCursor(user =>
                    {
                        Assert.True(user.Active);
                        verifySum += user.CustomerCode;
                        n++;
                    });
                    Console.WriteLine($"Read and verify completed ({i} time - {n} objects) in {sw.ElapsedMilliseconds} ms");
                    Assert.Equal(sum, verifySum);
                }
            }
            Console.WriteLine($"Segment alive = {MemorySegment.AliveCount}");
            Console.WriteLine($"Segment Flushs = {MemorySegment.FlushCount}");
        }


        [Theory()]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.PersistantRepository)]
        public void AddAndCommitTransactionsLargeConcurrently(SegmentStoreMode mode)
        {
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);
            var sw = Stopwatch.StartNew();

            int threadCount = 5;

            var tasks = new Task[threadCount];

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() => {
                    for (int j = 0; j < 10_000; j++)
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                var user = new BloggerUser()
                                {
                                    Active = true,
                                };
                            }
                            BloggerContext.Commit(true);
                        }
                });
            }

            Task.WaitAll(tasks);
            Console.WriteLine($"Write and commit users in {sw.ElapsedMilliseconds} ms");
            using (BloggerContext.NewReadContext(repository))
            {
                for (int i = 0; i < 3; i++)
                {
                    var n = 0;
                    sw = Stopwatch.StartNew();
                    BloggerUserCollection.ForEachCursor(user =>
                    {
                        Assert.True(user.Active);
                        n++;
                    });
                    Console.WriteLine($"Read and verify completed ({i} time - {n} objects) in {sw.ElapsedMilliseconds} ms");
                }
            }
            Console.WriteLine($"Segment alive = {MemorySegment.AliveCount}");
        }
#endif

        [Theory()]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.PersistantRepository)]
        public void AddAndCommitTransactionsConcurrentReadWrite(SegmentStoreMode mode)
        {
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);
            var sw = Stopwatch.StartNew();

            int threadCount = 4;
            long totalReads = 0;
            long totalWriters = 4;

            var tasks = new Task[threadCount * 2];

            var nTxn = 50_000;
            var nObjTxn = 500; // Set to 500 for large, 100M entries test
            if (true) // Set to true for quicker test
                nObjTxn = 20;
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() => {
                    for (int j = 0; j < nTxn; j++)
                        using (BloggerContext.NewWriteContext(repository))
                        {
                            if (j % (nTxn / 10) == 0)
                            {
                                //Thread.Sleep(5000);
                                Console.WriteLine($"Writer {threadId} at {j} / 50000");
                            }
                            for (int i = 0; i < nObjTxn; i++)
                            {
                                var user = new BloggerUser()
                                {
                                    Active = true,
                                };
                            }
                            BloggerContext.Commit(true);
                        }
                    Interlocked.Decrement(ref totalWriters);
                });
            }

            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i + threadCount] = Task.Run(() => {
                    var totalRetrieved = 0;
                    var retries = 0;
                    var lastCount = 0;
                    var countCount = 0;
                    while (totalRetrieved < threadCount * nTxn * nObjTxn)
                    {
                        retries++;
                        using (BloggerContext.NewReadContext(repository))
                        {
                            var n = 0;
                            sw = Stopwatch.StartNew();
                            BloggerUserCollection.ForEachCursor(user =>
                            {
                                Assert.True(user.Active);
                                n++;
                            });
                            Interlocked.Add(ref totalReads, n);
                            totalRetrieved = n;
                            if (n != lastCount)
                            {
                                lastCount = n;
                                countCount++;
                            }
                            //Console.WriteLine($"Read and verify completed ({i} time - {n} objects) in {sw.ElapsedMilliseconds} ms");
                        }
                    }
                    Console.WriteLine($"Retreived {totalRetrieved} objects after {retries} retry with {countCount} seen lenghts !");
                });
            }
            Task.WaitAll(tasks);
            Console.WriteLine($"Segment alive = {MemorySegment.AliveCount}");
            Console.WriteLine($"Total reads = {totalReads}");
        }
    }
}
