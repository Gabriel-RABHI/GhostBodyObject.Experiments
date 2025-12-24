using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Constants;
using GhostBodyObject.Common.Objects;
using System;
using System.Collections.Generic;
using System.Text;

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
