using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;

namespace GhostBodyObject.Common.Benchmarks.Objects
{
    public class GhostIdBenchmarks : BenchmarkBase
    {
        private const int COUNT = 10_000_000;

        [BruteForceBenchmark("OBJ-01", "GhostId vs GUID", "Objects")]
        public void SequentialTest()
        {
            var r1 = RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var id = GhostId.NewId(GhostIdKind.Entity, 1234);
                }
            })
            .PrintToConsole($"Create {COUNT:N0} GhostId")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();

            var r2 = RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var id = Guid.NewGuid();
                }
            })
            .PrintToConsole($"Create {COUNT:N0} GUID")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();

            PrintComparison("GhostId vs GUID", "", new BenchmarkResult[] { r1, r2 });
        }
    }
}
