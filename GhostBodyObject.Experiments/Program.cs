using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Experiments.BabyBody;
using System.Threading.Tasks;

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

    [BruteForceBenchmark("S01", "Create customers -> set one bool", "CPU")]
    public void CreateInstances()
    {
        RunMonitoredAction(() =>
        {
            Customer cust;
            for (int i = 0; i < COUNT; i++)
            {
                cust = new Customer();
                cust.Active = true;
            }
        })
        .PrintToConsole($"Customer {COUNT:N0} times")
        .PrintDelayPerOp(COUNT)
        .PrintSpace();

        RunMonitoredAction(() =>
        {
            PocoCustomer cust;
            for (int i = 0; i < COUNT; i++)
            {
                cust = new PocoCustomer();
                cust.Active = true;
            }
        })
        .PrintToConsole($"PocoCustomer {COUNT:N0} times")
        .PrintDelayPerOp(COUNT)
        .PrintSpace();
    }

    [BruteForceBenchmark("S02", "Set and get gool value", "CPU")]
    public void GetSetValue()
    {
        RunMonitoredAction(() =>
        {
            Customer cust;
            cust = new Customer();
            for (int i = 0; i < COUNT; i++)
            {
                for (int j = 0; j < 1000; j++)
                    cust.Active = !cust.Active;
            }
        })
        .PrintToConsole($"Customer {COUNT * 1000:N0} times")
        .PrintDelayPerOp(COUNT * 1000)
        .PrintSpace();

        RunMonitoredAction(() =>
        {
            PocoCustomer cust;
            cust = new PocoCustomer();
            for (int i = 0; i < COUNT; i++)
            {
                for (int j = 0; j < 1000; j++)
                    cust.Active = !cust.Active;
            }
        })
        .PrintToConsole($"PocoCustomer {COUNT * 1000:N0} times")
        .PrintDelayPerOp(COUNT * 1000)
        .PrintSpace();
    }

    [BruteForceBenchmark("S03", "Create customers -> Get string", "CPU")]
    public void GetString()
    {
        var count = COUNT * 100;
        RunMonitoredAction(() =>
        {
            Customer cust = new Customer();
            var sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += cust.CustomerName.Length;
            }
        })
        .PrintToConsole($"Customer {count:N0} times")
        .PrintDelayPerOp(count)
        .PrintSpace();

        RunMonitoredAction(() =>
        {
            PocoCustomer cust = new PocoCustomer() { CustomerName = "" };
            var sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += cust.CustomerName.Length;
            }
        })
        .PrintToConsole($"PocoCustomer {count:N0} times")
        .PrintDelayPerOp(count)
        .PrintSpace();
    }
}