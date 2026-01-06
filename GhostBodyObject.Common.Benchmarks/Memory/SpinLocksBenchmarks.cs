using GhostBodyObject.BenchmarkRunner;
using GhostBodyObject.Common.SpinLocks;
using System.Collections.Generic;
using System.Threading;

namespace GhostBodyObject.Common.Benchmarks.Memory
{
    public class SpinLocksBenchmarks : BenchmarkBase
    {
        private const int OperationsPerThread = 10_000_000;
        private const int ContentionOperationsPerThread = 1_000_000;

        #region Basic Lock Comparison

        /// <summary>
        /// Compares basic exclusive locks: ShortSpinLock, ShortNonSpinnedLock, .NET SpinLock, and Monitor (lock).
        /// </summary>
        [BruteForceBenchmark("SL-01", "Basic Exclusive Locks Comparison", "SpinLocks")]
        public void CompareBasicLocks()
        {
            var threadCounts = GetThreadCounts();

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                int sharedCounter = 0;

                // ShortSpinLock
                var shortSpinLock = new ShortSpinLock();
                sharedCounter = 0;
                var shortSpinResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        shortSpinLock.Enter();
                        sharedCounter++;
                        shortSpinLock.Exit();
                    }
                });
                shortSpinResult.WithLabel("ShortSpinLock").WithOperations(totalOps);
                results.Add(shortSpinResult);

                // .NET SpinLock
                var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: false);
                sharedCounter = 0;
                var dotnetSpinResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        bool lockTaken = false;
                        dotnetSpinLock.Enter(ref lockTaken);
                        sharedCounter++;
                        if (lockTaken) dotnetSpinLock.Exit();
                    }
                });
                dotnetSpinResult.WithLabel(".NET SpinLock").WithOperations(totalOps);
                results.Add(dotnetSpinResult);

                // Monitor (lock statement)
                var lockObject = new object();
                sharedCounter = 0;
                var monitorResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        lock (lockObject)
                        {
                            sharedCounter++;
                        }
                    }
                });
                monitorResult.WithLabel("Monitor (lock)").WithOperations(totalOps);
                results.Add(monitorResult);

                PrintComparison(
                    $"Basic Exclusive Locks - {threadCount} thread(s)",
                    $"{OperationsPerThread:N0} lock/unlock cycles per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        #endregion

        #region Ticket Lock Comparison

        /// <summary>
        /// Compares ShortTicketSpinLock with other locks for fairness scenarios.
        /// </summary>
        [BruteForceBenchmark("SL-02", "Ticket Lock vs Standard Locks (FIFO Fairness)", "SpinLocks")]
        public void CompareTicketLocks()
        {
            var threadCounts = GetThreadCounts();

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                int sharedCounter = 0;

                // ShortTicketSpinLock
                var ticketLock = new ShortTicketSpinLock();
                sharedCounter = 0;
                var ticketResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        ticketLock.Enter();
                        sharedCounter++;
                        ticketLock.Exit();
                    }
                });
                ticketResult.WithLabel("ShortTicketSpinLock").WithOperations(totalOps);
                results.Add(ticketResult);

                // ShortSpinLock for comparison
                var shortSpinLock = new ShortSpinLock();
                sharedCounter = 0;
                var shortSpinResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        shortSpinLock.Enter();
                        sharedCounter++;
                        shortSpinLock.Exit();
                    }
                });
                shortSpinResult.WithLabel("ShortSpinLock").WithOperations(totalOps);
                results.Add(shortSpinResult);

                // .NET SpinLock
                var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: false);
                sharedCounter = 0;
                var dotnetSpinResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        bool lockTaken = false;
                        dotnetSpinLock.Enter(ref lockTaken);
                        sharedCounter++;
                        if (lockTaken) dotnetSpinLock.Exit();
                    }
                });
                dotnetSpinResult.WithLabel(".NET SpinLock").WithOperations(totalOps);
                results.Add(dotnetSpinResult);

                PrintComparison(
                    $"Ticket Lock Comparison - {threadCount} thread(s)",
                    $"{OperationsPerThread:N0} lock/unlock cycles per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        #endregion

        #region Recursive Lock Comparison

        /// <summary>
        /// Compares ShortRecursiveSpinLock with .NET SpinLock (with thread tracking) for recursive scenarios.
        /// </summary>
        [BruteForceBenchmark("SL-03", "Recursive Locks Comparison", "SpinLocks")]
        public void CompareRecursiveLocks()
        {
            var threadCounts = GetThreadCounts();
            const int recursionDepth = 3;

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                int sharedCounter = 0;

                // ShortRecursiveSpinLock
                var recursiveLock = new ShortRecursiveSpinLock();
                sharedCounter = 0;
                var recursiveResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        // Simulate recursive entry
                        for (int d = 0; d < recursionDepth; d++)
                            recursiveLock.Enter();
                        sharedCounter++;
                        for (int d = 0; d < recursionDepth; d++)
                            recursiveLock.Exit();
                    }
                });
                recursiveResult.WithLabel("ShortRecursiveSpinLock").WithOperations(totalOps);
                results.Add(recursiveResult);

                // .NET SpinLock with thread owner tracking (simulating recursion check)
                var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: true);
                sharedCounter = 0;
                var dotnetSpinResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        bool lockTaken = false;
                        dotnetSpinLock.Enter(ref lockTaken);
                        // Note: .NET SpinLock doesn't support true recursion, so we just do one enter/exit
                        sharedCounter++;
                        if (lockTaken) dotnetSpinLock.Exit();
                    }
                });
                dotnetSpinResult.WithLabel(".NET SpinLock (tracked)").WithOperations(totalOps);
                results.Add(dotnetSpinResult);

                // Monitor (naturally recursive)
                var lockObject = new object();
                sharedCounter = 0;
                var monitorResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        for (int d = 0; d < recursionDepth; d++)
                            Monitor.Enter(lockObject);
                        sharedCounter++;
                        for (int d = 0; d < recursionDepth; d++)
                            Monitor.Exit(lockObject);
                    }
                });
                monitorResult.WithLabel("Monitor (recursive)").WithOperations(totalOps);
                results.Add(monitorResult);

                PrintComparison(
                    $"Recursive Locks - {threadCount} thread(s) (depth={recursionDepth})",
                    $"{OperationsPerThread:N0} lock/unlock cycles per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        #endregion

        #region Reader-Writer Lock Comparison

        /// <summary>
        /// Compares ShortReadWriteSpinLock with .NET ReaderWriterLockSlim.
        /// </summary>
        [BruteForceBenchmark("SL-04", "Reader-Writer Locks Comparison", "SpinLocks")]
        public void CompareReadWriteLocks()
        {
            var threadCounts = GetThreadCounts();

            foreach (var threadCount in threadCounts)
            {
                // Skip if we have less than 2 threads (need at least readers and writers)
                if (threadCount < 2) continue;

                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                int sharedValue = 0;

                // ShortReadWriteSpinLock - Read Heavy (90% reads, 10% writes)
                var rwSpinLock = new ShortReadWriteSpinLock();
                sharedValue = 0;
                var rwSpinReadResult = RunParallelAction(threadCount, threadId =>
                {
                    bool isWriter = threadId == 0; // Only first thread writes
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (isWriter && (i % 10 == 0))
                        {
                            rwSpinLock.EnterWrite();
                            sharedValue++;
                            rwSpinLock.ExitWrite();
                        }
                        else
                        {
                            rwSpinLock.EnterRead();
                            _ = sharedValue;
                            rwSpinLock.ExitRead();
                        }
                    }
                });
                rwSpinReadResult.WithLabel("ShortRWSpinLock").WithOperations(totalOps);
                results.Add(rwSpinReadResult);

                // .NET ReaderWriterLockSlim
                var rwLockSlim = new ReaderWriterLockSlim();
                sharedValue = 0;
                var rwSlimResult = RunParallelAction(threadCount, threadId =>
                {
                    bool isWriter = threadId == 0;
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (isWriter && (i % 10 == 0))
                        {
                            rwLockSlim.EnterWriteLock();
                            sharedValue++;
                            rwLockSlim.ExitWriteLock();
                        }
                        else
                        {
                            rwLockSlim.EnterReadLock();
                            _ = sharedValue;
                            rwLockSlim.ExitReadLock();
                        }
                    }
                });
                rwSlimResult.WithLabel("ReaderWriterLockSlim").WithOperations(totalOps);
                results.Add(rwSlimResult);

                rwLockSlim.Dispose();

                PrintComparison(
                    $"Reader-Writer Locks - {threadCount} thread(s) (90% read, 10% write)",
                    $"{OperationsPerThread:N0} ops per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        /// <summary>
        /// Compares Reader-Writer locks under write-heavy workload.
        /// </summary>
        [BruteForceBenchmark("SL-05", "Reader-Writer Locks Write-Heavy Comparison", "SpinLocks")]
        public void CompareReadWriteLocksWriteHeavy()
        {
            var threadCounts = GetThreadCounts();

            foreach (var threadCount in threadCounts)
            {
                if (threadCount < 2) continue;

                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                int sharedValue = 0;

                // ShortReadWriteSpinLock - Write Heavy (50% writes)
                var rwSpinLock = new ShortReadWriteSpinLock();
                sharedValue = 0;
                var rwSpinResult = RunParallelAction(threadCount, threadId =>
                {
                    bool isWriter = threadId % 2 == 0; // Half threads write
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (isWriter)
                        {
                            rwSpinLock.EnterWrite();
                            sharedValue++;
                            rwSpinLock.ExitWrite();
                        }
                        else
                        {
                            rwSpinLock.EnterRead();
                            _ = sharedValue;
                            rwSpinLock.ExitRead();
                        }
                    }
                });
                rwSpinResult.WithLabel("ShortRWSpinLock").WithOperations(totalOps);
                results.Add(rwSpinResult);

                // .NET ReaderWriterLockSlim
                var rwLockSlim = new ReaderWriterLockSlim();
                sharedValue = 0;
                var rwSlimResult = RunParallelAction(threadCount, threadId =>
                {
                    bool isWriter = threadId % 2 == 0;
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (isWriter)
                        {
                            rwLockSlim.EnterWriteLock();
                            sharedValue++;
                            rwLockSlim.ExitWriteLock();
                        }
                        else
                        {
                            rwLockSlim.EnterReadLock();
                            _ = sharedValue;
                            rwLockSlim.ExitReadLock();
                        }
                    }
                });
                rwSlimResult.WithLabel("ReaderWriterLockSlim").WithOperations(totalOps);
                results.Add(rwSlimResult);

                rwLockSlim.Dispose();

                PrintComparison(
                    $"RW Locks Write-Heavy - {threadCount} thread(s) (50% write)",
                    $"{OperationsPerThread:N0} ops per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        #endregion

        #region Count-Limited Lock (Semaphore-like)

        /// <summary>
        /// Compares ShortCountSpinLock with .NET SemaphoreSlim.
        /// </summary>
        [BruteForceBenchmark("SL-06", "Count-Limited Locks (Semaphore) Comparison", "SpinLocks")]
        public void CompareCountLocks()
        {
            var threadCounts = GetThreadCounts();
            const int maxConcurrency = 4;

            foreach (var threadCount in threadCounts)
            {
                if (threadCount < 2) continue;

                var results = new List<BenchmarkResult>();
                long totalOps = (long)ContentionOperationsPerThread * threadCount;
                int sharedCounter = 0;

                // ShortCountSpinLock
                var countLock = new ShortCountSpinLock(maxConcurrency);
                sharedCounter = 0;
                var countResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < ContentionOperationsPerThread; i++)
                    {
                        countLock.Enter();
                        Interlocked.Increment(ref sharedCounter);
                        countLock.Exit();
                    }
                });
                countResult.WithLabel($"ShortCountSpinLock({maxConcurrency})").WithOperations(totalOps);
                results.Add(countResult);

                // .NET SemaphoreSlim
                using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                sharedCounter = 0;
                var semaphoreResult = RunParallelAction(threadCount, _ =>
                {
                    for (int i = 0; i < ContentionOperationsPerThread; i++)
                    {
                        semaphore.Wait();
                        Interlocked.Increment(ref sharedCounter);
                        semaphore.Release();
                    }
                });
                semaphoreResult.WithLabel($"SemaphoreSlim({maxConcurrency})").WithOperations(totalOps);
                results.Add(semaphoreResult);

                PrintComparison(
                    $"Count-Limited Locks - {threadCount} thread(s) (max {maxConcurrency} concurrent)",
                    $"{ContentionOperationsPerThread:N0} ops per thread, {totalOps:N0} total ops",
                    results);
            }
        }

        #endregion

        #region Uncontended Performance (Single Thread)

        /// <summary>
        /// Measures uncontended lock acquisition performance (no contention).
        /// </summary>
        [BruteForceBenchmark("SL-07", "Uncontended Lock Performance (Single Thread)", "SpinLocks")]
        public void UncontendedLockPerformance()
        {
            const int iterations = 100_000_000;
            var results = new List<BenchmarkResult>();
            int dummy = 0;

            WriteComment($"Single-threaded uncontended lock performance: {iterations:N0} iterations");

            // ShortSpinLock
            var shortSpinLock = new ShortSpinLock();
            var shortSpinResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    shortSpinLock.Enter();
                    dummy++;
                    shortSpinLock.Exit();
                }
            });
            shortSpinResult.WithLabel("ShortSpinLock").WithOperations(iterations);
            results.Add(shortSpinResult);

            // ShortTicketSpinLock
            var ticketLock = new ShortTicketSpinLock();
            var ticketResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    ticketLock.Enter();
                    dummy++;
                    ticketLock.Exit();
                }
            });
            ticketResult.WithLabel("ShortTicketSpinLock").WithOperations(iterations);
            results.Add(ticketResult);

            // ShortRecursiveSpinLock
            var recursiveLock = new ShortRecursiveSpinLock();
            var recursiveResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    recursiveLock.Enter();
                    dummy++;
                    recursiveLock.Exit();
                }
            });
            recursiveResult.WithLabel("ShortRecursiveSpinLock").WithOperations(iterations);
            results.Add(recursiveResult);

            // .NET SpinLock (no tracking)
            var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: false);
            var dotnetSpinResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    bool lockTaken = false;
                    dotnetSpinLock.Enter(ref lockTaken);
                    dummy++;
                    if (lockTaken) dotnetSpinLock.Exit();
                }
            });
            dotnetSpinResult.WithLabel(".NET SpinLock").WithOperations(iterations);
            results.Add(dotnetSpinResult);

            // .NET SpinLock (with tracking)
            var dotnetSpinLockTracked = new SpinLock(enableThreadOwnerTracking: true);
            var dotnetSpinTrackedResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    bool lockTaken = false;
                    dotnetSpinLockTracked.Enter(ref lockTaken);
                    dummy++;
                    if (lockTaken) dotnetSpinLockTracked.Exit();
                }
            });
            dotnetSpinTrackedResult.WithLabel(".NET SpinLock (tracked)").WithOperations(iterations);
            results.Add(dotnetSpinTrackedResult);

            // Monitor (lock)
            var lockObject = new object();
            var monitorResult = RunMonitoredAction(() =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    lock (lockObject)
                    {
                        dummy++;
                    }
                }
            });
            monitorResult.WithLabel("Monitor (lock)").WithOperations(iterations);
            results.Add(monitorResult);

            PrintComparison(
                "Uncontended Lock Performance",
                $"{iterations:N0} lock/unlock cycles, single thread",
                results);
        }

        #endregion

        #region High Contention Stress Test

        /// <summary>
        /// Stress tests locks under high contention with all CPU cores.
        /// </summary>
        [BruteForceBenchmark("SL-08", "High Contention Stress Test", "SpinLocks")]
        public void HighContentionStressTest()
        {
            int maxThreads = Environment.ProcessorCount;
            var results = new List<BenchmarkResult>();
            long totalOps = (long)ContentionOperationsPerThread * maxThreads;
            int sharedCounter = 0;

            WriteComment($"High contention test: {maxThreads} threads, {ContentionOperationsPerThread:N0} ops each");

            // ShortSpinLock
            var shortSpinLock = new ShortSpinLock();
            sharedCounter = 0;
            var shortSpinResult = RunParallelAction(maxThreads, _ =>
            {
                for (int i = 0; i < ContentionOperationsPerThread; i++)
                {
                    shortSpinLock.Enter();
                    sharedCounter++;
                    shortSpinLock.Exit();
                }
            });
            shortSpinResult.WithLabel("ShortSpinLock").WithOperations(totalOps);
            results.Add(shortSpinResult);

            // ShortTicketSpinLock
            var ticketLock = new ShortTicketSpinLock();
            sharedCounter = 0;
            var ticketResult = RunParallelAction(maxThreads, _ =>
            {
                for (int i = 0; i < ContentionOperationsPerThread; i++)
                {
                    ticketLock.Enter();
                    sharedCounter++;
                    ticketLock.Exit();
                }
            });
            ticketResult.WithLabel("ShortTicketSpinLock").WithOperations(totalOps);
            results.Add(ticketResult);

            // .NET SpinLock
            var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: false);
            sharedCounter = 0;
            var dotnetSpinResult = RunParallelAction(maxThreads, _ =>
            {
                for (int i = 0; i < ContentionOperationsPerThread; i++)
                {
                    bool lockTaken = false;
                    dotnetSpinLock.Enter(ref lockTaken);
                    sharedCounter++;
                    if (lockTaken) dotnetSpinLock.Exit();
                }
            });
            dotnetSpinResult.WithLabel(".NET SpinLock").WithOperations(totalOps);
            results.Add(dotnetSpinResult);

            // Monitor (lock)
            var lockObject = new object();
            sharedCounter = 0;
            var monitorResult = RunParallelAction(maxThreads, _ =>
            {
                for (int i = 0; i < ContentionOperationsPerThread; i++)
                {
                    lock (lockObject)
                    {
                        sharedCounter++;
                    }
                }
            });
            monitorResult.WithLabel("Monitor (lock)").WithOperations(totalOps);
            results.Add(monitorResult);

            PrintComparison(
                $"High Contention - {maxThreads} threads (all cores)",
                $"{ContentionOperationsPerThread:N0} ops per thread, {totalOps:N0} total ops",
                results);
        }

        #endregion

        #region TryEnter Performance

        /// <summary>
        /// Compares TryEnter/TryLock performance for non-blocking lock acquisition.
        /// </summary>
        [BruteForceBenchmark("SL-09", "TryEnter/TryLock Non-Blocking Performance", "SpinLocks")]
        public void TryEnterPerformance()
        {
            var threadCounts = GetThreadCounts();

            foreach (var threadCount in threadCounts)
            {
                var results = new List<BenchmarkResult>();
                long totalOps = (long)OperationsPerThread * threadCount;
                long successCount = 0;

                // ShortSpinLock TryEnter
                var shortSpinLock = new ShortSpinLock();
                successCount = 0;
                var shortSpinResult = RunParallelAction(threadCount, _ =>
                {
                    int localSuccess = 0;
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (shortSpinLock.TryEnter())
                        {
                            localSuccess++;
                            shortSpinLock.Exit();
                        }
                    }
                    Interlocked.Add(ref successCount, localSuccess);
                });
                shortSpinResult.WithLabel("ShortSpinLock.TryEnter").WithOperations(totalOps);
                results.Add(shortSpinResult);

                // .NET SpinLock TryEnter
                var dotnetSpinLock = new SpinLock(enableThreadOwnerTracking: false);
                successCount = 0;
                var dotnetSpinResult = RunParallelAction(threadCount, _ =>
                {
                    int localSuccess = 0;
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        bool lockTaken = false;
                        dotnetSpinLock.TryEnter(ref lockTaken);
                        if (lockTaken)
                        {
                            localSuccess++;
                            dotnetSpinLock.Exit();
                        }
                    }
                    Interlocked.Add(ref successCount, localSuccess);
                });
                dotnetSpinResult.WithLabel(".NET SpinLock.TryEnter").WithOperations(totalOps);
                results.Add(dotnetSpinResult);

                // Monitor.TryEnter
                var lockObject = new object();
                successCount = 0;
                var monitorResult = RunParallelAction(threadCount, _ =>
                {
                    int localSuccess = 0;
                    for (int i = 0; i < OperationsPerThread; i++)
                    {
                        if (Monitor.TryEnter(lockObject))
                        {
                            localSuccess++;
                            Monitor.Exit(lockObject);
                        }
                    }
                    Interlocked.Add(ref successCount, localSuccess);
                });
                monitorResult.WithLabel("Monitor.TryEnter").WithOperations(totalOps);
                results.Add(monitorResult);

                PrintComparison(
                    $"TryEnter Performance - {threadCount} thread(s)",
                    $"{OperationsPerThread:N0} attempts per thread, {totalOps:N0} total attempts",
                    results);
            }
        }

        #endregion

        #region Helper Methods

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

            // Ensure we include the max processor count if it's not already a power of 2
            if (counts.Count > 0 && counts[^1] != maxThreads && maxThreads > counts[^1])
            {
                int highestPow2 = 1;
                while (highestPow2 * 2 <= maxThreads)
                    highestPow2 *= 2;

                if (!counts.Contains(highestPow2))
                    counts.Add(highestPow2);
            }

            return [.. counts];
        }

        #endregion
    }
}
