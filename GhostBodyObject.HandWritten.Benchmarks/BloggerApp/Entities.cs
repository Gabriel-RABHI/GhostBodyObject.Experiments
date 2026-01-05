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
                RunMonitoredAction(() =>
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
            RunMonitoredAction(() =>
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
        }


        [BruteForceBenchmark("OBJ-02", "Create BloggerUser in mass", "Objects")]
        public void SequentialMemoryReclaim()
        {
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {

                RunMonitoredAction(() =>
                {
                    for (int j = 0; j < 100; j++)
                        for (int i = 0; i < COUNT; i++)
                        {
                            var user = new BloggerUser();
                            user.FirstName = "Ted is in the wild.";
                        }
                })
                .PrintToConsole($"Set strings for {COUNT * 100:N0} BloggerUser")
                .PrintDelayPerOp(COUNT * 100)
                .PrintSpace();
            }

            RunMonitoredAction(() =>
            {
                //var user = pocousers[0];
                for (int j = 0; j < 100; j++)
                    for (int i = 0; i < COUNT; i++)
                    {
                        var user = new UserPOCO();
                        user.FirstName = "Ted is in the wild.";
                    }
            })
            .PrintToConsole($"Set strings for {COUNT * 100:N0} UserPOCO")
            .PrintDelayPerOp(COUNT * 100)
            .PrintSpace();
        }

        [BruteForceBenchmark("OBJ-03", "Garbage Collection time", "Objects")]
        public void GarbageCollection()
        {
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
                RunGCCollect("Collect #1 retainned BloggerUser.");
                RunGCCollect("Collect #2 retainned BloggerUser.");
                Thread.Sleep(1000);
            }
            Console.WriteLine("Done." + users.Count);
            users.Clear();

            RunGCCollect("Release #1 retainned BloggerUser.");
            RunGCCollect("Release #2 retainned BloggerUser.");

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
            RunGCCollect("Collect #1 retainned UserPOCO.");
            RunGCCollect("Collect #2 retainned UserPOCO.");
            Console.WriteLine("Done." + pocousers.Count);

            Thread.Sleep(10000);

            pocousers.Clear();

            RunGCCollect("Release #1 retainned UserPOCO.");
            RunGCCollect("Release #2 retainned UserPOCO.");
        }

        public void RunGCCollect(string prompt)
        {
            RunMonitoredAction(() =>
            {
                // Request compaction of Large Object Heap
                //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

                // First pass: collect all generations
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                // Wait for all finalizers to complete
                GC.WaitForPendingFinalizers();

                // Second pass: collect objects that were moved from finalization queue
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

                // Final wait to ensure everything is complete
                GC.WaitForPendingFinalizers();
            })
            .PrintToConsole(prompt)
            .PrintDelayPerOp(COUNT * 10)
            .PrintSpace();
        }
    }
}
