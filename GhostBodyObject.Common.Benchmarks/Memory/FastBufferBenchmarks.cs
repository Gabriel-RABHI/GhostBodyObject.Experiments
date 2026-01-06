using GhostBodyObject.BenchmarkRunner;

namespace GhostBodyObject.Common.Benchmarks.Memory
{
    internal class FastBufferBenchmarks : BenchmarkBase
    {
        private const int COUNT = 1000_000_000;

        [BruteForceBenchmark("FB-01", "FastBuffer - Get values comparison", "Memory")]
        public unsafe void GetValues()
        {
            byte[] rawBuffer = new byte[128];
            fixed (byte* target = rawBuffer)
            {
                var pinned = target;
                var r1 = RunMonitoredAction(() =>
                {
                    var ptr = pinned;
                    long sum = 0;
                    for (int i = 0; i < COUNT; i++)
                    {
                        sum += FastBuffer.Get<int>(ptr, 8);
                    }
                })
                .PrintToConsole($"Get value {COUNT:N0} - direct byte*")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                var r2 = RunMonitoredAction(() =>
                {
                    PinnedMemory<byte> pinnedMemory = new PinnedMemory<byte>(rawBuffer, pinned, rawBuffer.Length);
                    long sum = 0;
                    for (int i = 0; i < COUNT; i++)
                    {
                        sum += FastBuffer.Get<int>(pinnedMemory, 8);
                    }
                })
                .PrintToConsole($"Get value {COUNT:N0} - using PinnedMemory<T>")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                var r3 = RunMonitoredAction(() =>
                {
                    PinnedMemory<byte> pinnedMemory = new PinnedMemory<byte>(rawBuffer, pinned, rawBuffer.Length);
                    long sum = 0;
                    for (int i = 0; i < COUNT; i++)
                    {
                        sum += pinnedMemory.Get<int>(8);
                    }
                })
                .PrintToConsole($"Get value {COUNT:N0} - using PinnedMemory<T>.Get()")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                PrintComparison("Get unmanaged memory", "Compare the various code to read a value at a arbitrary memory location", new BenchmarkResult[] { r1, r2, r3 });
            }
        }

        [BruteForceBenchmark("FB-02", "FastBuffer - Set values comparison", "Memory")]
        public unsafe void SetValues()
        {
            byte[] rawBuffer = new byte[128];
            fixed (byte* target = rawBuffer)
            {
                var pinned = target;
                var r1 = RunMonitoredAction(() =>
                {
                    var ptr = pinned;
                    for (int i = 0; i < COUNT; i++)
                    {
                        FastBuffer.Set(ptr, 8, i);
                    }
                })
                .PrintToConsole($"Set value {COUNT:N0} - direct byte*")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                var r2 = RunMonitoredAction(() =>
                {
                    PinnedMemory<byte> pinnedMemory = new PinnedMemory<byte>(rawBuffer, pinned, rawBuffer.Length);
                    for (int i = 0; i < COUNT; i++)
                    {
                        FastBuffer.Set(pinnedMemory, 8, i);
                    }
                })
                .PrintToConsole($"Set value {COUNT:N0} - using PinnedMemory<T>")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                var r3 = RunMonitoredAction(() =>
                {
                    PinnedMemory<byte> pinnedMemory = new PinnedMemory<byte>(rawBuffer, pinned, rawBuffer.Length);
                    for (int i = 0; i < COUNT; i++)
                    {
                        pinnedMemory.Set(8, i);
                    }
                })
                .PrintToConsole($"Set value {COUNT:N0} - using PinnedMemory<T>.Set()")
                .PrintDelayPerOp(COUNT)
                .PrintSpace();

                PrintComparison("Set unmanaged memory", "Compare the various code to write a value at a arbitrary memory location", new BenchmarkResult[] { r1, r2, r3 });
            }
        }
    }
}
