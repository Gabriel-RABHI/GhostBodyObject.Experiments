using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;

namespace GhostBodyObject.HandWritten.Tests.BloggerApp
{
    public class BloggerContextShould
    {
        [Fact]
        public void OpenAndAssignTransactions()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();

                user.Active = true;
                user.Active = false;
                
                for (int i=0; i < 200_000_000; i++)
                {
                    user.FirstName = "John - " + i;
                }
                /*
                for (int i = 0; i < 200_000_000; i++)
                {
                    user.FirstNameString = "John - " + i;
                }
                */
                var txn = BloggerContext.Transaction;
                using (BloggerContext.OpenReadContext(repository))
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

        public class PocoBloggerUser
        {
            public bool Active { get; set; }
            public string FirstName { get; set; }

        }

        [Fact]
        public void AssignPoco()
        {
                var user = new PocoBloggerUser();
                for (int i = 0; i < 200_000_000; i++)
                {
                    user.FirstName = "John - " + i;
                }
        }
    }
}
