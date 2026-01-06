using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

/*

-> Serial Modifications

-> GC pressure


Il y a deux optimisations possibles :
1) Faire une implémentations sans Setter func-ptr
2) Eviter les GhostString : passer en String.

Mais le fond c'est :
- Ces hyper optimisations sont-elles vraiment utiles en pratique ?
- La souplesse des Func-Ptrs est-elle vraiment préférable ?


*/

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
            BenchmarkResult r1 = null;
            var users = new List<BloggerUser>();
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    users.Add(new BloggerUser());
                }
                r1 = RunMonitoredAction(() =>
                {
                    //var user = users[0];
                    for (int j = 0; j < 10; j++)
                        for (int i = 0; i < COUNT; i++)
                        {
                            var user = users[i];
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
                .PrintToConsole($"Set strings for {COUNT * 10:N0} BloggerUser")
                .PrintDelayPerOp(COUNT * 10)
                .PrintSpace();
            }

            var pocousers = new List<UserPOCO>();
            for (int i = 0; i < COUNT; i++)
                pocousers.Add(new UserPOCO());
            var r2 = RunMonitoredAction(() =>
            {
                //var user = pocousers[0];
                for (int j = 0; j < 10; j++)
                    for (int i = 0; i < COUNT; i++)
                    {
                        var user = pocousers[i];
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
            .PrintToConsole($"Set strings for {COUNT * 10:N0} UserPOCO")
            .PrintDelayPerOp(COUNT * 10)
            .PrintSpace();

            PrintComparison("Body / POCO", "BloggerUser performance comparison with POCO", new BenchmarkResult[] { r1, r2 });
        }


        [BruteForceBenchmark("OBJ-02", "Create BloggerUser in mass", "Objects")]
        public void SequentialMemoryReclaim()
        {
            BenchmarkResult r1 = null;
            BenchmarkResult r2 = null;
            var repository = new BloggerRepository();

            WriteComment("The array retain 32 objects at any time, prevent the stack alloc.");
            WriteComment("The single assignation create a standalone Ghost memory block.");

            using (BloggerContext.OpenReadContext(repository))
            {
                var array = new BloggerUser[32];
                r1 = RunMonitoredAction(() =>
                {
                    for (int j = 0; j < 100; j++)
                        for (int i = 0; i < COUNT; i++)
                        {
                            var user = new BloggerUser();
                            array[i% 32] = user;
                        }
                })
                .PrintToConsole($"Create {COUNT * 100:N0} initial BloggerUser")
                .PrintDelayPerOp(COUNT * 100)
                .PrintSpace();
                WriteComment($"{(array.Count(o => o.FirstName.Length > 0))}");
            }

            using (BloggerContext.OpenReadContext(repository))
            {
                var array = new BloggerUser[32];
                r2 = RunMonitoredAction(() =>
                {
                    for (int j = 0; j < 100; j++)
                        for (int i = 0; i < COUNT; i++)
                        {
                            var user = new BloggerUser();
                            user.FirstName = "Ted is in the wild.";
                            array[i % 32] = user;
                        }
                })
                .PrintToConsole($"Set strings for {COUNT * 100:N0} BloggerUser")
                .PrintDelayPerOp(COUNT * 100)
                .PrintSpace();
                WriteComment($"{(array.Count(o => o.FirstName.Length > 0))}");
            }

            var arrayPoco = new UserPOCO[32];
            var r3 = RunMonitoredAction(() =>
            {
                for (int j = 0; j < 100; j++)
                    for (int i = 0; i < COUNT; i++)
                    {
                        var user = new UserPOCO();
                        user.FirstName = "Ted is in the wild.";
                        arrayPoco[i % 32] = user;
                    }
            })
            .PrintToConsole($"Set strings for {COUNT * 100:N0} UserPOCO")
            .PrintDelayPerOp(COUNT * 100)
            .PrintSpace();

            WriteComment($"{(arrayPoco.Count(o => o.FirstName != null))}");

            PrintComparison("Body / POCO", "Create BloggerUser in mass", new BenchmarkResult[] { r1, r2, r3 });
        }

        [BruteForceBenchmark("OBJ-03", "Garbage Collection time", "Objects")]
        public void GarbageCollection()
        {
            BenchmarkResult r1 = null;
            var users = new List<BloggerUser>();
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var user = new BloggerUser();
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
                    users.Add(user);
                }
                r1 = RunGCCollect("Collect retainned BloggerUser.");
                Thread.Sleep(1000);
            }
            users.Clear();

            var r2 = RunGCCollect("Release retainned BloggerUser.");

            Thread.Sleep(1000);

            var pocousers = new List<UserPOCO>();
            for (int i = 0; i < COUNT; i++)
            {
                var user = new UserPOCO();
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
                pocousers.Add(user);
            }
            var r3 = RunGCCollect("Collect retainned UserPOCO.");

            Thread.Sleep(10000);

            pocousers.Clear();

            var r4 =RunGCCollect("Release retainned UserPOCO.");

            PrintComparison("Body / POCO", "Retainned Garbage collection delay", new BenchmarkResult[] { r1, r3 });
            PrintComparison("Body / POCO", "Garbage collection delay", new BenchmarkResult[] { r2, r4 });
        }

        public BenchmarkResult RunGCCollect(string prompt)
        {
            return RunMonitoredAction(() =>
            {
                // First pass: collect all generations
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                // Wait for all finalizers to complete
                GC.WaitForPendingFinalizers();
            })
            .PrintToConsole(prompt)
            .PrintDelayPerOp(COUNT * 10)
            .PrintSpace();
        }
    }
}
