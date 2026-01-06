using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.BloggerApp.Entities.UserFlat;

namespace GhostBodyObject.HandWritten.Tests.BloggerAll
{
    public class BloggerFlatEntitiesShould
    {
        [Fact]
        public void ChangeValueProperty()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUserFlat();

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

        [Fact(Skip ="All Properties are not implemented.")]
        public void ChangeStringProperty()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUserFlat();

                Assert.Equal("", user.FirstName);
                user.FirstName = "John";
                Assert.Equal("John", user.FirstName);

                Assert.Equal("", user.LastName);

                user.LastName = "Travolta";
                Assert.Equal("Travolta", user.LastName);

                user.Pseudonyme = "JT";
                Assert.Equal("JT", user.Pseudonyme);

                user.Presentation = "One of the most iconic actor.";
                Assert.Equal("One of the most iconic actor.", user.Presentation);

                user.City = "New York";
                Assert.Equal("New York", user.City);

                user.Country = "USA";
                Assert.Equal("USA", user.Country);

                user.CompanyName = "Independant Actor";
                Assert.Equal("Independant Actor", user.CompanyName);

                user.Address1 = "Somewhere";
                Assert.Equal("Somewhere", user.Address1);

                user.Address2 = "A street";
                Assert.Equal("A street", user.Address2);

                user.Address3 = "Well...";
                Assert.Equal("Well...", user.Address3);

                user.ZipCode = "4538-45";
                Assert.Equal("4538-45", user.ZipCode);

                user.Hobbies = "American Cinema";
                Assert.Equal("American Cinema", user.Hobbies);

                // ---------- //

                user.FirstName = "Bill";
                Assert.Equal("Bill", user.FirstName);

                Assert.Equal("Travolta", user.LastName);
                user.LastName = "Gates";
                Assert.Equal("Gates", user.LastName);

                Assert.Equal("JT", user.Pseudonyme);

                Assert.Equal("One of the most iconic actor.", user.Presentation);
                user.Presentation = "Microsoft Founder.";
                Assert.Equal("Microsoft Founder.", user.Presentation);

                Assert.Equal("Bill", user.FirstName);
                Assert.Equal("New York", user.City);
                Assert.Equal("USA", user.Country);
                Assert.Equal("Independant Actor", user.CompanyName);
                Assert.Equal("Somewhere", user.Address1);
                Assert.Equal("A street", user.Address2);
                Assert.Equal("Well...", user.Address3);
                Assert.Equal("4538-45", user.ZipCode);
                Assert.Equal("American Cinema", user.Hobbies);
            }
        }
    }
}
