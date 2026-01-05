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

        [Fact]
        public void ChangeStringPropertyAndReadBackZeroCopy()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();
                user.Active = true;
                user.FirstName = "John-Mayer-Travolta-of-the-moon";

                for (int i = 0; i < 100; i++)
                {
                    var v = (user.FirstName = "John-Mayer-Travolta-of-the-moon" + i).ToString();
                    for (int j = 0; j < 1_000_000; j++)
                        Assert.True(user.FirstName.Equals(v));
                }
            }
        }

        [Fact]
        public void ChangeStringPropertyAndReadBackAsString()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();
                user.Active = true;
                user.FirstNameString = "John-Mayer-Travolta-of-the-moon";
                Assert.Equal("John-Mayer-Travolta-of-the-moon", user.FirstNameString);

                for (int i = 0; i < 100; i++)
                {
                    var v = user.FirstNameString = "John-Mayer-Travolta-of-the-moon" + i;
                    for (int j = 0; j < 1_000_000; j++)
                        Assert.True(user.FirstNameString == v);
                }
            }
        }

        [Fact]
        public void ChangeStringPropertyMillionTimes()
        {
            var strings = new string[] { "John-Mayer-Travolta-of-the-moon", "Alice-in-Wonderland-on-Mars", "Bob-the-Builder-in-Space", "Charlie-and-the-Chocolate-Factory-on-Venus" };
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();
                user.Active = true;
                user.FirstName = "John-Mayer-Travolta-of-the-moon";
                Assert.Equal("John-Mayer-Travolta-of-the-moon", user.FirstName);

                for (int i = 0; i < 500_000_000; i++)
                {
                    user.FirstName = strings[i & 0x02];
                }
            }
        }

        [Fact]
        public void ChangeValuePropertyMillionTimes()
        {
            var bools = new bool[] { true, false, true, false };
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                var user = new BloggerUser();
                user.Active = true;
                for (int i = 0; i < 2_000_000_000; i++)
                {
                    user.Active = bools[i & 0x02];
                }
            }
        }
    }
}
