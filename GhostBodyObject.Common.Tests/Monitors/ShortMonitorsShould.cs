using GhostBodyObject.Common.SpinLocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Common.Tests.SpinLocks
{

    public class ShortSpinLocksShould
    {
        [Theory(DisplayName = "Protect critical sections using ShortSpinLock")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void ProtectCriticalSectionsWithShortSpinLock(int nThreads)
        {
            var SpinLock = new ShortSpinLock();
            var count = 0;
#if RELEASE
            var loops = 3_000_000;
#else
            var loops = 100_000;
#endif
            var action = () =>
            {
                for (int i = 0; i < loops; i++)
                {
                    SpinLock.Enter();
                    try
                    {
                        var r = Interlocked.Increment(ref count);
                        Assert.True(r < 2);
                        Interlocked.Decrement(ref count);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        SpinLock.Exit();
                    }
                }
            };

            var actions = new List<Action>();
            for (int i = 0; i < nThreads; i++)
                actions.Add(action);

            await Task.WhenAll(actions.Select(a => Task.Run(a)).ToArray());
        }

        [Theory(DisplayName = "Protect critical sections using ShortNonSpinnedSpinLock")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void ProtectCriticalSectionsWithShortNonSpinnedSpinLock(int nThreads)
        {
            var SpinLock = new ShortNonSpinnedLock();
            var count = 0;
#if RELEASE
            var loops = 3_000_000;
#else
            var loops = 100_000;
#endif

            var action = () =>
            {
                for (int i = 0; i < loops; i++)
                {
                    SpinLock.Enter();
                    try
                    {
                        var r = Interlocked.Increment(ref count);
                        Assert.True(r < 2);
                        Interlocked.Decrement(ref count);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        SpinLock.Exit();
                    }
                }
            };

            var actions = new List<Action>();
            for (int i = 0; i < nThreads; i++)
                actions.Add(action);

            await Task.WhenAll(actions.Select(a => Task.Run(a)).ToArray());
        }

        [Theory(DisplayName = "Allow exact thread count to enter using ShortCountSpinLock")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void AllowExactThreadCountToEnterUsingShortCountSpinLock(int nThreads)
        {
            var SpinLock = new ShortCountSpinLock(2);
            var count = 0;
#if RELEASE
            var loops = 3_000_000;
#else
            var loops = 100_000;
#endif

            var action = () =>
            {
                for (int i = 0; i < loops; i++)
                {
                    SpinLock.Enter();
                    try
                    {
                        var r = Interlocked.Increment(ref count);
                        Assert.True(r < 3);
                        Interlocked.Decrement(ref count);
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        SpinLock.Exit();
                    }
                }
            };

            var actions = new List<Action>();
            for (int i = 0; i < nThreads; i++)
                actions.Add(action);

            await Task.WhenAll(actions.Select(a => Task.Run(a)).ToArray());
        }

        [Theory(DisplayName = "Allow thread recursive enter using ShortRecursiveSpinLock")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void AllowThreadRecursiveEnterUsingShortRecursiveSpinLock(int nThreads)
        {
            var SpinLock = new ShortRecursiveSpinLock();
            var count = 0;
#if RELEASE
            var loops = 3_000_000;
#else
            var loops = 100_000;
#endif

            var action = () =>
            {
                for (int i = 0; i < loops; i++)
                {
                    SpinLock.Enter();
                    try
                    {
                        var r = Interlocked.Increment(ref count);
                        Assert.True(r < 2);
                        SpinLock.Enter();
                        try
                        {
                            Assert.True(count < 2);
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            SpinLock.Exit();
                            Interlocked.Decrement(ref count);
                        }
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        SpinLock.Exit();
                    }
                }
            };

            var actions = new List<Action>();
            for (int i = 0; i < nThreads; i++)
                actions.Add(action);

            await Task.WhenAll(actions.Select(a => Task.Run(a)).ToArray());
        }

        [Theory(DisplayName = "Allow one writer multiple reader using ShortReadWriteSpinLock")]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public async void AllowOneWriterMultipleReaderUsingShortReadWriteSpinLock(int nThreads)
        {
            var SpinLock = new ShortReadWriteSpinLock();
            var countWrite = 0;
#if RELEASE
            var loops = 3_000_000;
#else
            var loops = 100_000;
#endif
            var action = () =>
            {
                for (int i = 0; i < loops; i++)
                {
                    if (i % 15 == 0)
                    {
                        SpinLock.EnterWrite();
                        try
                        {
                            var r = Interlocked.Increment(ref countWrite);
                            Assert.True(r < 2);
                            Interlocked.Decrement(ref countWrite);
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            SpinLock.ExitWrite();
                        }
                    }
                    else
                    {
                        SpinLock.EnterRead();
                        try
                        {
                            Assert.Equal(0, Volatile.Read(ref countWrite));
                        }
                        catch
                        {
                            throw;
                        }
                        finally
                        {
                            SpinLock.ExitRead();
                        }
                    }
                }
            };

            var actions = new List<Action>();
            for (int i = 0; i < nThreads; i++)
                actions.Add(action);

            await Task.WhenAll(actions.Select(a => Task.Run(a)).ToArray());
        }
    }
}
