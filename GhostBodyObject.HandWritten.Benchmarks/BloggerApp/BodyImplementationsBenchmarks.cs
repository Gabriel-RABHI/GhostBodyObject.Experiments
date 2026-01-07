using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;

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
    public class BodyImplementationsBenchmarks : BenchmarkBase
    {
        private const int COUNT = 2_000_000_000;
        private const int COUNT_STR = 200_000_000;

        [BruteForceBenchmark("OBJ-05", "FuncPtr based vs direct setter", "Objects")]
        public void SequentialTest()
        {

            var bools = new bool[] { true, false, true, false };
            var strings = new string[] { "John", "Alice", "Bob", "Charlie" };
            var repository = new BloggerRepository();
            using (BloggerContext.OpenReadContext(repository))
            {
                RunMonitoredAction(() =>
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    for (int i = 0; i < COUNT; i++)
                    {
                        user.Active = bools[i & 0x02];
                    }
                })
                .PrintToConsole($"Set value for {COUNT:N0} BloggerUser")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();


                RunMonitoredAction(() =>
                {
                    var user = new BloggerUser();
                    user.Active = true;
                    for (int i = 0; i < COUNT_STR; i++)
                    {
                        user.FirstName = strings[i & 0x02];
                    }
                })
                .PrintToConsole($"Set strings for {COUNT_STR:N0} BloggerUser")
                .PrintDelayPerOp(COUNT_STR)
                .PrintSpace();
            }
        }
    }
}
