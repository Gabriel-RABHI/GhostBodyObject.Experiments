using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.HandWritten.Benchmarks.BloggerApp
{
    public sealed class UserPOCO
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Pseudonyme { get; set; }
        public string Presentation { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string CompanyName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string ZipCode { get; set; }
        public string Hobbies { get; set; }
        public bool Active { get; set; }

    }


    public class GhostIdBenchmarks : BenchmarkBase
    {
        private const int COUNT = 1_000_000;

        [BruteForceBenchmark("OBJ-01", "BloggerUser performance comparison with POCO", "Objects")]
        public void SequentialTest()
        {
            var users = new List<BloggerUser>();
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
                    user.FirstName = "John";
                    user.Pseudonyme = "JT";
                    user.City = "New York";
                    user.Country = "USA";
                    user.CompanyName = "Independant Actor";
                    user.Address1 = "Somewhere";
                    user.Address2 = "A street";
                    user.Address3 = "Well...";
                    user.ZipCode = "4538-45";
                    user.Hobbies = "American Cinema";
                    user.LastName = "Travolta";
                    user.Presentation = "One of the most iconic actor.";
                    users.Add(new BloggerUser());
                }
                RunMonitoredAction(() =>
                {
                    var user = users[0];
                    for (int i = 0; i < COUNT; i++)
                    {
                        //var user = users[i];
                        var istring = i.ToString();
                        user.FirstName = "John" + istring;
                        user.Pseudonyme = "JT" + istring;
                        user.City = "New York" + istring;
                        user.Country = "USA" + istring;
                        user.CompanyName = "Independant Actor" + istring;
                        user.Address1 = "Somewhere" + istring;
                        user.Address2 = "A street" + istring;
                        user.Address3 = "Well..." + istring;
                        user.ZipCode = "4538-45" + istring;
                        user.Hobbies = "American Cinema" + istring;
                        user.LastName = "Travolta" + istring;
                        user.Presentation = "One of the most iconic actor." + istring;
                    }
                })
                .PrintToConsole($"Set strings for {COUNT:N0} BloggerUser")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();


            }

            var pocousers = new List<UserPOCO>();
            for (int i = 0; i < COUNT; i++)
                pocousers.Add(new UserPOCO());
            RunMonitoredAction(() =>
            {
                var user = pocousers[0];
                for (int i = 0; i < COUNT; i++)
                {
                    //var user = pocousers[i];
                    var istring = i.ToString();
                    user.FirstName = "John" + istring;
                    user.Pseudonyme = "JT" + istring;
                    user.City = "New York" + istring;
                    user.Country = "USA" + istring;
                    user.CompanyName = "Independant Actor" + istring;
                    user.Address1 = "Somewhere" + istring;
                    user.Address2 = "A street" + istring;
                    user.Address3 = "Well..." + istring;
                    user.ZipCode = "4538-45" + istring;
                    user.Hobbies = "American Cinema" + istring;
                    user.LastName = "Travolta" + istring;
                    user.Presentation = "One of the most iconic actor." + istring;
                }
            })
            .PrintToConsole($"Set strings for {COUNT:N0} UserPOCO")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();
        }
    }
}
