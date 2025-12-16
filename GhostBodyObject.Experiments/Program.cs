using System.Threading.Tasks;
using GhostBodyObject.BenchmarkRunner;

class Program
{
    static async Task Main(string[] args)
    {
        // One line start
        await BenchmarkEngine.DiscoverAndShowAsync();
    }
}

// -------------------------------------------------------------
// SAMPLE BENCHMARK CLASS
// -------------------------------------------------------------
public class SampleBenchmarks : BenchmarkBase
{
    private const int COUNT = 1_000_000;
    private const int THREAD_COUNT = 8;

    [BruteForceBenchmark("S01", "Standard Sequential Loop", "CPU")]
    public void SequentialTest()
    {
        RunMonitoredAction(() =>
        {
            double sum = 0;
            for (int i = 0; i < COUNT; i++)
            {
                sum += Math.Sqrt(i);
            }
        })
        .PrintToConsole($"Math.Sqrt {COUNT:N0} times")
        .PrintDelayPerOp(COUNT)
        .PrintSpace();
    }

    [BruteForceBenchmark("P01", "Parallel String Allocation", "Memory")]
    public void ParallelTest()
    {
        RunParallelAction(THREAD_COUNT, (threadId) =>
        {
            // Tight loop inside user delegate
            for (int i = 0; i < COUNT; i++)
            {
                string s = "test" + i;
            }
        })
        .PrintToConsole($"Allocating {COUNT * THREAD_COUNT:N0} strings")
        .PrintDelayPerOp(COUNT * THREAD_COUNT)
        .PrintSpace();

        

        RunParallelAction(THREAD_COUNT, (threadId) =>
        {
            // Tight loop inside user delegate
            for (int i = 0; i < COUNT; i++)
            {
                string s = "test" + i;
            }
            
        })
        .PrintToConsole($"Allocating {COUNT * THREAD_COUNT:N0} strings")
        .PrintDelayPerOp(COUNT * THREAD_COUNT)
        .PrintSpace();
    }
}