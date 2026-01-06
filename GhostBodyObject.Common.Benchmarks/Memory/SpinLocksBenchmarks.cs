using GhostBodyObject.BenchmarkRunner;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Common.Benchmarks.Memory
{
    public class SpinLocksBenchmarks : BenchmarkBase
    {
        [BruteForceBenchmark("SL-01", "ShortSpinLock vs Lock()", "SpinLocks")]
        public unsafe void SetValues()
        {
        }
    }
}
