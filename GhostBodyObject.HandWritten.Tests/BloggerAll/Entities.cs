using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.HandWritten.Tests.BloggerAll
{
    public class BloggerEntitiesShould
    {
        [Fact]
        public void ChangeValueProperty()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();

                user.Active = true;
                Assert.True(user.Active);

                user.Active = false;
                Assert.False(user.Active);

                user.CustomerCode = 12;
                Assert.Equal(12, user.CustomerCode);

                var now = user.BirthDate = DateTime.Now;
                Assert.Equal(now, user.BirthDate);
            }
        }

        [Fact]
        public void ChangeStringProperty()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();
                user.FirstName = "John";
                Assert.Equal("John", user.FirstName);
            }
        }
    }
}
