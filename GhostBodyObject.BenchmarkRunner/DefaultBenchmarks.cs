namespace GhostBodyObject.BenchmarkRunner
{
    // -------------------------------------------------------------
    // SAMPLE BENCHMARK CLASS
    // -------------------------------------------------------------
    public class DefaultBenchmarks : BenchmarkBase
    {
        [BruteForceBenchmark("RunAll", "Run All Benchmarks", "Z-SYS")]
        public async Task RunAllBenchmarks()
        {
            await BenchmarkEngine.RunAllBenchmarksAsync();
        }

        [BruteForceBenchmark("GC", "Run GC Collect", "Z-SYS")]
        public void SequentialTest()
        {
            RunMonitoredAction(() =>
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.WaitForFullGCComplete();
            })
            .PrintToConsole($"GC.Collect")
            .PrintSpace();
        }

        [BruteForceBenchmark("", "Quit", "Z-SYS")]
        public void Quit()
        {
            Environment.Exit(0);
        }

        [BruteForceBenchmark("", "Switch Outputs Generation", "Z-SYS")]
        public void ToogleGenerateOutput()
        {
            BenchmarkEngine.GenerateOutputFiles = !BenchmarkEngine.GenerateOutputFiles;
            WriteComment($"{(BenchmarkEngine.GenerateOutputFiles ? "File output activated" : "File output disabled")}");
        }
    }
}