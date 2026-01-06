using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.Memory;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Common.Benchmarks.Memory
{
    public class MemoryAllocatorBenchmarks : BenchmarkBase
    {
        private const int AllocationsPerThread = 1_000_000;
        private static readonly int[] BlockSizes = [100, 256, 512, 1024];

        /// <summary>
        /// Compares TransientGhostMemoryAllocator, NativeMemory, and byte[] allocators
        /// across different block sizes and thread counts.
        /// </summary>
        [BruteForceBenchmark("MEM-ALLOC-CMP", "Memory Allocators Comparison (Multi-threaded)", "Memory")]
        public void CompareAllocators()
        {
            // Determine thread counts based on processor count (powers of 2)
            var threadCounts = GetThreadCounts();
            
            foreach (var blockSize in BlockSizes)
            {
                WriteComment($"Block Size: {blockSize} bytes - {AllocationsPerThread:N0} allocations per thread");

                foreach (var threadCount in threadCounts)
                {
                    var results = new List<BenchmarkResult>();
                    long totalOps = (long)AllocationsPerThread * threadCount;

                    // TransientGhostMemoryAllocator
                    var ghostResult = RunParallelAction(threadCount, _ =>
                    {
                        for (int i = 0; i < AllocationsPerThread; i++)
                        {
                            var mem = TransientGhostMemoryAllocator.Allocate(blockSize);
                            //var mem = ManagedArenaAllocator.Allocate((int)blockSize);
                            // Touch memory to ensure allocation is real
                            if (!mem.IsEmpty)
                                mem[0] = (byte)(i & 0xFF);
                        }
                    });
                    ghostResult.WithLabel("Ghost").WithOperations(totalOps);
                    results.Add(ghostResult);

                    // Native Memory (NativeMemory.Alloc/Free)
                    var nativeResult = RunParallelAction(threadCount, _ =>
                    {
                        for (int i = 0; i < AllocationsPerThread; i++)
                        {
                            unsafe
                            {
                                var ptr = NativeMemory.Alloc((nuint)blockSize);
                                // Touch memory to ensure allocation is real
                                ((byte*)ptr)[0] = (byte)(i & 0xFF);
                                NativeMemory.Free(ptr);
                            }
                        }
                    });
                    nativeResult.WithLabel("Native").WithOperations(totalOps);
                    results.Add(nativeResult);

                    // .NET byte[] allocator
                    var dotnetResult = RunParallelAction(threadCount, _ =>
                    {
                        for (int i = 0; i < AllocationsPerThread; i++)
                        {
                            var arr = new byte[blockSize];
                            // Touch memory to ensure allocation is real
                            arr[0] = (byte)(i & 0xFF);
                        }
                    });
                    dotnetResult.WithLabel("byte[]").WithOperations(totalOps);
                    results.Add(dotnetResult);

                    PrintComparison(
                        $"{blockSize} bytes - {threadCount} thread(s)",
                        $"{AllocationsPerThread:N0} allocations/thread, {totalOps:N0} total ops",
                        results);
                }
            }
        }

        /// <summary>
        /// Compares allocators focusing on allocation throughput per allocator type.
        /// </summary>
        [BruteForceBenchmark("MEM-ALLOC-TPUT", "Memory Allocators Throughput", "Memory")]
        public void AllocatorThroughput()
        {
            var threadCounts = GetThreadCounts();
            const int blockSize = 512;

            WriteComment($"Throughput comparison at {blockSize} bytes block size");

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)AllocationsPerThread * threadCount;

                // TransientGhostMemoryAllocator
                var ghostResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < AllocationsPerThread; i++)
                    {
                        var mem = TransientGhostMemoryAllocator.Allocate(blockSize);
                        if (!mem.IsEmpty) mem[0] = (byte)(i & 0xFF);
                    }
                });
                ghostResult.WithLabel("Ghost").WithOperations(totalOps);
                results.Add(ghostResult);

                // Native Memory
                var nativeResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < AllocationsPerThread; i++)
                    {
                        unsafe
                        {
                            var ptr = NativeMemory.Alloc((nuint)blockSize);
                            ((byte*)ptr)[0] = (byte)(i & 0xFF);
                            NativeMemory.Free(ptr);
                        }
                    }
                });
                nativeResult.WithLabel("Native").WithOperations(totalOps);
                results.Add(nativeResult);

                // .NET byte[]
                var dotnetResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < AllocationsPerThread; i++)
                    {
                        var arr = new byte[blockSize];
                        arr[0] = (byte)(i & 0xFF);
                    }
                });
                dotnetResult.WithLabel("byte[]").WithOperations(totalOps);
                results.Add(dotnetResult);

                PrintComparison(
                    $"Throughput - {threadCount} thread(s)",
                    $"{blockSize} bytes blocks, {totalOps:N0} total allocations",
                    results);
            }
        }

        /// <summary>
        /// Tests allocation and resize patterns common in real usage.
        /// </summary>
        [BruteForceBenchmark("MEM-ALLOC-RESIZE", "Memory Allocators with Resize", "Memory")]
        public void AllocateAndResize()
        {
            var threadCounts = GetThreadCounts();
            const int resizeIterations = 10_000;

            WriteComment($"Allocation + Resize pattern: {resizeIterations:N0} iterations per thread");

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)resizeIterations * threadCount;

                // TransientGhostMemoryAllocator with resize
                var ghostResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < resizeIterations; i++)
                    {
                        var mem = TransientGhostMemoryAllocator.Allocate(100);
                        TransientGhostMemoryAllocator.Resize(ref mem, 256);
                        TransientGhostMemoryAllocator.Resize(ref mem, 512);
                        TransientGhostMemoryAllocator.Resize(ref mem, 1024);
                        if (!mem.IsEmpty) mem[0] = (byte)(i & 0xFF);
                    }
                });
                ghostResult.WithLabel("Ghost Resize").WithOperations(totalOps);
                results.Add(ghostResult);

                // Native Memory with realloc pattern
                var nativeResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < resizeIterations; i++)
                    {
                        unsafe
                        {
                            var ptr = NativeMemory.Alloc(100);
                            ptr = NativeMemory.Realloc(ptr, 256);
                            ptr = NativeMemory.Realloc(ptr, 512);
                            ptr = NativeMemory.Realloc(ptr, 1024);
                            ((byte*)ptr)[0] = (byte)(i & 0xFF);
                            NativeMemory.Free(ptr);
                        }
                    }
                });
                nativeResult.WithLabel("Native Realloc").WithOperations(totalOps);
                results.Add(nativeResult);

                // .NET with Array.Resize pattern
                var dotnetResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < resizeIterations; i++)
                    {
                        var arr = new byte[100];
                        Array.Resize(ref arr, 256);
                        Array.Resize(ref arr, 512);
                        Array.Resize(ref arr, 1024);
                        arr[0] = (byte)(i & 0xFF);
                    }
                });
                dotnetResult.WithLabel("Array.Resize").WithOperations(totalOps);
                results.Add(dotnetResult);

                PrintComparison(
                    $"Resize Pattern - {threadCount} thread(s)",
                    $"100 -> 256 -> 512 -> 1024 bytes, {resizeIterations:N0} iterations/thread",
                    results);
            }
        }

        /// <summary>
        /// Gets thread counts as powers of 2 up to processor count.
        /// </summary>
        private static int[] GetThreadCounts()
        {
            int maxThreads = Environment.ProcessorCount;
            var counts = new List<int>();
            
            for (int t = 1; t <= maxThreads; t *= 2)
            {
                counts.Add(t);
            }
            
            // Ensure we include the max processor count if it's not already included
            if (counts.Count > 0 && counts[^1] != maxThreads && maxThreads > counts[^1])
            {
                // Find the highest power of 2 <= maxThreads
                int highestPow2 = 1;
                while (highestPow2 * 2 <= maxThreads)
                    highestPow2 *= 2;
                
                if (!counts.Contains(highestPow2))
                    counts.Add(highestPow2);
            }
            
            return [.. counts];
        }
    }
}
