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
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository, false)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository, false)]
        [InlineData(SegmentStoreMode.PersistantRepository, false)]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository, true)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository, true)]
        [InlineData(SegmentStoreMode.PersistantRepository, true)]
        public void AddAndCommitTransactions(SegmentStoreMode mode, bool cursor)
        {
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);

            Action<Action<BloggerUser>> forEach = cursor  ? ((a) => BloggerCollections.BloggerUsers.Scan(a)) : ((a) => BloggerCollections.BloggerUsers.ForEach(a));
            // ================================================================ //
            // -------- Mutations
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
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                    n++;
                });
                Assert.Equal(2, n);
                BloggerContext.Commit(false);
            }
            // -------- Validate
            using (BloggerContext.NewReadContext(repository))
            {
                forEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                });
            }
            using (BloggerContext.NewWriteContext(repository))
            {
                forEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                });
            }
            // ================================================================ //
            // -------- Mutations
            using (BloggerContext.NewWriteContext(repository))
            {
                // -------- Cursor aggregation work
                var names = BloggerCollections.BloggerUsers.Cursor.Select(u => u.FirstName.ToString()).ToList();
                Assert.Equal(1, names.Count(name => name == "John"));
                Assert.Equal(1, names.Count(name => name == "Ted"));

                // -------- Cursor sellect fails
                var users = BloggerCollections.BloggerUsers.Cursor.Select(u => u).ToList();
                Assert.Equal(0, users.Count(u => u.FirstName == "John"));
                Assert.Equal(2, users.Count(u => u.FirstName == "Ted"));

                // -------- Cursor sellect work
                users = BloggerCollections.BloggerUsers.Instances.Select(u => u).ToList();
                Assert.Equal(0, users.Count(u => u.FirstName == "John"));
                Assert.Equal(2, users.Count(u => u.FirstName == "Ted"));

                var n = 0;
                BloggerCollections.BloggerUsers.Scan(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                    user.City = "New York City";
                    n++;
                });
                Assert.Equal(2, n);

                var list = BloggerCollections.BloggerUsers.Cursor.Where(u => u.City == "New York City").ToList();
                Assert.Equal(1, list.Count(u => u.FirstName == "John"));
                Assert.Equal(1, list.Count(u => u.FirstName == "Ted"));

                n = 0;
                forEach(user =>
                {
                    Assert.True(user.City == "New York City");
                    n++;
                });
                Assert.Equal(2, n);
                BloggerContext.Commit(false);
            }
            // -------- Validate
            using (BloggerContext.NewReadContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.City == "New York City");
                    n++;
                });

                var list = BloggerCollections.BloggerUsers.Where(u => u.City == "New York City").ToList();
                Assert.Equal(1, list.Where(u => u.FirstName == "John").Count());
                Assert.Equal(1, list.Where(u => u.FirstName == "Ted").Count());

                Assert.Equal(2, n);
            }
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.City == "New York City");
                    n++;
                });
                Assert.Equal(2, n);
            }
            // ================================================================ //
            // -------- Mutations
            // Rollback test
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    user.City = "Paris";
                    n++;
                });
                Assert.Equal(2, n);
                BloggerContext.Rollback();
            }
            // -------- Validate
            using (BloggerContext.NewReadContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.City == "New York City");
                    n++;
                });
                Assert.Equal(2, n);
            }
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.City == "New York City");
                    n++;
                });
                Assert.Equal(2, n);
            }
            // ================================================================ //
            // -------- Mutations
            // Delete test
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    if (user.FirstName == "John")
                        user.Delete();
                    n++;
                });
                Assert.Equal(2, n);
                n = 0;
                forEach(user =>
                {
                    Assert.True(user.FirstName == "Ted");
                    n++;
                });
                Assert.Equal(1, n);
                BloggerContext.Commit();
            }
            // -------- Validate
            using (BloggerContext.NewReadContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.FirstName == "Ted");
                    n++;
                });
                Assert.Equal(1, n);
            }
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                forEach(user =>
                {
                    Assert.True(user.FirstName == "Ted");
                    n++;
                });
                Assert.Equal(1, n);
            }
        }

        [Theory()]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository)]
        [InlineData(SegmentStoreMode.PersistantRepository)]
        public void AddAndCommitTransactionsAndModify(SegmentStoreMode mode)
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
                var n = 0;
                BloggerCollections.BloggerUsers.ForEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                    n++;
                });
                Assert.Equal(2, n);
                BloggerContext.Commit(false);
            }
            using (BloggerContext.NewWriteContext(repository))
            {
                var n = 0;
                BloggerCollections.BloggerUsers.ForEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                    user.City = "New York City";
                    n++;
                });
                Assert.Equal(2, n);
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
                    BloggerCollections.BloggerUsers.Scan(user =>
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
                    BloggerCollections.BloggerUsers.Scan(user =>
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
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository, false)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository, false)]
        [InlineData(SegmentStoreMode.PersistantRepository, false)]
        [InlineData(SegmentStoreMode.InMemoryVolatileRepository, true)]
        [InlineData(SegmentStoreMode.InVirtualMemoryVolatileRepository, true)]
        [InlineData(SegmentStoreMode.PersistantRepository, true)]
        public void AddAndCommitTransactionsConcurrentReadWrite(SegmentStoreMode mode, bool cursor)
        {
            Action<Action<BloggerUser>> forEach = cursor ? ((a) => BloggerCollections.BloggerUsers.Scan(a)) : ((a) => BloggerCollections.BloggerUsers.ForEach(a));
            using var tempDir = new TempDirectoryHelper(true);
            using var repository = new BloggerRepository(mode, tempDir.DirectoryPath);
            var sw = Stopwatch.StartNew();

            int threadCount = 4;
            long totalReads = 0;
            long totalWriters = 4;

            var tasks = new Task[threadCount * 2];

            // -------- 100M entries test
            var nTxn = 50_000;
            var nObjTxn = 500; 
            if (true)
            {
                // -------- 4M entries test - faster test
                nTxn = 2_500;
                nObjTxn = 400;
            }
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
                                Console.WriteLine($"Writer {threadId} at {j} / {nTxn}");
                            }
                            for (int i = 0; i < nObjTxn; i++)
                            {
                                var user = new BloggerUser()
                                {
                                    Active = true,
                                };
                            }
                            BloggerContext.Commit(false);
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
                            forEach(user =>
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
