using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Benchmarks.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Benchmarks.BloggerApp;
using GhostBodyObject.Repository.Benchmarks.Ghosts;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var types = new Type[] {
            typeof(FastBufferBenchmarks),
            typeof(MemoryAllocatorBenchmarks),
            typeof(SpinLocksBenchmarks),
            typeof(BodyVsPOCOBenchmarks),
            typeof(SegmentGhostMapBenchmarks),
            typeof(BodyImplementationsBenchmarks),
            typeof(BodyVsPOCOBenchmarks)
        };

        foreach (var item in types)
        {
            Console.WriteLine($"Loaded : {item}");
        }

        // One line start
        await BenchmarkEngine.DiscoverAndShowAsync();
    }
}