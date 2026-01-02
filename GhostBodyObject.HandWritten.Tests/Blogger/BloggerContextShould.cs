using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Entities;
using GhostBodyObject.HandWritten.Blogger.Repository;

namespace GhostBodyObject.HandWritten.Tests.Blogger
{
    public class BloggerContextShould
    {
        [Fact]
        public void OpenAndAssignTransactions()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenContext(repository))
            {
                var user = new BloggerUser();
                var txn = BloggerContext.Transaction;
                using (BloggerContext.OpenContext(repository))
                {
                    var user2 = new BloggerUser();
                    var txn2 = BloggerContext.Transaction;

                    Assert.True(txn != txn2);
                    Assert.True(user.Transaction != txn2);
                    Assert.True(user2.Transaction != txn);
                    Assert.True(user2.Transaction == txn2);
                    BloggerContext.Transaction.Commit();
                }
                Assert.True(BloggerContext.Transaction == txn);
            }
        }
    }
}
