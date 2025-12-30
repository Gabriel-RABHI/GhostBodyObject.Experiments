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
            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var id = GhostId.NewId(GhostIdKind.Entity, 1234);
                }
            })
            .PrintToConsole($"Create {COUNT:N0} GhostId")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();

            RunMonitoredAction(() =>
            {
                for (int i = 0; i < COUNT; i++)
                {
                    var id = Guid.NewGuid();
                }
            })
            .PrintToConsole($"Create {COUNT:N0} GUID")
            .PrintDelayPerOp(COUNT)
            .PrintSpace();
        }
    }
}
