using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using System.Diagnostics;
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
            var COUNT = 100_000_000;
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

        [Fact]
        public void AddAndCommitTransactions()
        {
            var repository = new BloggerRepository();
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
                BloggerContext.Transaction.Commit();
            }
            using (BloggerContext.NewReadContext(repository))
            {
                //Assert.True(BloggerContext.Transaction.BloggerUserCollection.Any());
                BloggerContext.Transaction.BloggerUserCollection.ForEach(user =>
                {
                    Assert.True(user.Active);
                    Assert.True(
                        (user.FirstName == "John" && user.LastName == "Doe") ||
                        (user.FirstName == "Ted" && user.LastName == "Smith"));
                });
            }
        }

#if RELEASE
        [Fact]
        public void AddAndCommitTransactionsLarge()
        {
            var repository = new BloggerRepository();
            var sw = Stopwatch.StartNew();
            long sum = 0;
            for (int j = 0; j < 1_000; j++)
                using (BloggerContext.NewWriteContext(repository))
                {
                    for (int i=0;i< 10_000; i++)
                    {
                        var user = new BloggerUser()
                        {
                            Active = true,
                            FirstName = "John" + i,
                            LastName = "Doe",
                            CustomerCode = i + (j % 99)
                        };
                        sum += user.CustomerCode;
                    }
                    BloggerContext.Transaction.Commit();
                }
            Console.WriteLine($"Write and commit users in {sw.ElapsedMilliseconds} ms");
            using (BloggerContext.NewReadContext(repository))
            {
                for (int i = 0; i < 3; i++)
                {
                    long verifySum = 0;
                    var n = 0;
                    sw = Stopwatch.StartNew();
                    //Assert.True(BloggerContext.Transaction.BloggerUserCollection.Any());
                    BloggerContext.Transaction.BloggerUserCollection.ForEachCursor(user =>
                    {
                        Assert.True(user.Active);
                        verifySum += user.CustomerCode;
                        n++;
                    });
                    Console.WriteLine($"Read and verify completed ({i} time - {n} objects) in {sw.ElapsedMilliseconds} ms");
                    Assert.Equal(sum, verifySum);
                }
            }
        }
#endif
    }
}
