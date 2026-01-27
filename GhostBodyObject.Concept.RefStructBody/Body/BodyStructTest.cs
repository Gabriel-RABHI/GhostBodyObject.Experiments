using GhostBodyObject.Common.Memory;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.HandWritten.Benchmarks.BloggerApp
{
    public struct LargeStruct
    {
        public long A;
        public long B;
        public long C;
        public long D;
        public long E;
        public long F;
        public long G;
        public long H;

        public bool Correct()
        {
            return B == A + 1 && C == B + 1 && D == C + 1 && E == D + 1 && F == E + 1 && G == F + 1;
        }
    }

    public unsafe struct BodyStructHandle : IDisposable
    {
        internal BodyStructSlot[] _arena;
        internal ushort _index;

        public ref BodyStructSlot Body {
            get {
                ref BodyStructSlot baseRef = ref MemoryMarshal.GetArrayDataReference(_arena);
                ref BodyStructSlot ptr = ref Unsafe.Add(ref baseRef, _index);
                return ref ptr;
            }
        }

        public LargeStruct OneBigValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                ref var _slot = ref Body;
                byte* ghost = null;
                IntPtr vtable;
                object owner;
                int seq1;
            _redo:
                do
                {
                    seq1 = Volatile.Read(ref _slot._sequence);
                    if ((seq1 & 1) != 0)
                    {
                        Thread.SpinWait(1);
                        goto _redo;
                    }
                    ghost = _slot._ghost;
                    vtable = _slot._vtable;
                    owner = _slot._ghostOwner;
                    Thread.MemoryBarrier();
                }
                while (Volatile.Read(ref _slot._sequence) != seq1);

                // BE CAREFULL : this works only because the ghost memory is immutable
                // At this position, the Setter is possibly starting a mutation : but we have a coherent (ghost + vtable + owner) field set
                //
                // The rules is : anytime we have to change one field (ghost / vtable / owner) value
                // it must be performer in between Increment of _sequence !

                int offset = 8;
                return *(LargeStruct*)(ghost + offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                ref var _slot = ref Body;
                var spin = new SpinWait();

                while (true)
                {
                    int seq = Volatile.Read(ref _slot._sequence);
                    if ((seq & 1) != 0)
                    {
                        spin.SpinOnce();
                        continue;
                    }
                    if (Interlocked.CompareExchange(ref _slot._sequence, seq + 1, seq) == seq)
                    {
                        try
                        {
                            var mm = TransientGhostMemoryAllocator.Allocate(96);

                            // BE CAREFULL : Any mutation "in" the elements can lead to stale value read and fail !
                            // The sync garantee only that all 3 fields are coherently grabbed by readers, not their content is coherent
                            int offset = 8;
                            *(LargeStruct*)(mm.Ptr + offset) = value;

                            _slot._ghost = mm.Ptr;
                            _slot._ghostOwner = mm.MemoryOwner;
                        } finally
                        {
                            Volatile.Write(ref _slot._sequence, seq + 2);
                        }
                        return; // Success
                    }
                    spin.SpinOnce();
                }
            }
        }

        public int OneIntValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                ref var _slot = ref Body;
                byte* ghost = null;
                IntPtr vtable;
                object owner;
                int seq1;
            _redo:
                do
                {
                    seq1 = Volatile.Read(ref _slot._sequence);
                    if ((seq1 & 1) != 0)
                    {
                        Thread.SpinWait(1);
                        goto _redo;
                    }
                    ghost = _slot._ghost;
                    vtable = _slot._vtable;
                    owner = _slot._ghostOwner;
                    Thread.MemoryBarrier();
                }
                while (Volatile.Read(ref _slot._sequence) != seq1);
                int offset = 8; // may be retreived using vtable
                return *(int*)(ghost + offset);
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public unsafe struct BodyStructSlot
    {
        public byte* _ghost;
        public IntPtr _vtable;
        public object _context;
        public object _ghostOwner;
        public int _sequence;
    }

    public static class BodyStructAllocator
    {
        public const int ArenaSize = 4096;

        private class ArenaPage
        {
            public readonly BodyStructSlot[] Slots = new BodyStructSlot[ArenaSize];

            public int Head = -1;
        }

        // Volatile ensures all threads see the most recent page immediately
        private static volatile ArenaPage _activePage = new ArenaPage();
        private static readonly object _lock = new object();

        public unsafe static BodyStructHandle AllocateNew()
        {
            while (true)
            {
                ArenaPage page = _activePage;
                int idx = Interlocked.Increment(ref page.Head);
                if (idx < ArenaSize)
                {
                    BodyStructHandle handle = new BodyStructHandle {
                        _arena = page.Slots,
                        _index = (ushort)idx
                    };
                    return handle;
                }
                lock (_lock)
                {
                    if (_activePage == page)
                    {
                        _activePage = new ArenaPage();
                    }
                }
            }
        }
    }


    public class TestBodyStructAccess
    {
        public void TestBodyStructAllocator()
        {
            var runs = 100_000_000;
            List<BodyStructHandle> list = new();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < runs; i++)
            {
                list.Add(BodyStructAllocator.AllocateNew());
            }
            Console.WriteLine($"Elapsed to create handles: {sw.ElapsedMilliseconds} ms");
            list.Clear();
            list = null;
        }

        public unsafe void Test()
        {
            var runs = 100_000_000;
            var h = BodyStructAllocator.AllocateNew();
            var mm = TransientGhostMemoryAllocator.Allocate(96);
            h.Body._ghost = mm.Ptr;
            h.Body._ghostOwner = mm.MemoryOwner;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var started = false;
            var a = Task.Run(() =>
            {
                var x = 0l;
                for (int i = 0; i < runs; i++)
                {
                    h.OneBigValue = new LargeStruct() {
                        A = x++,
                        B = x++,
                        C = x++,
                        D = x++,
                        E = x++,
                        F = x++,
                        G = x++,
                        H = x++
                    };
                    started = true;
                }
            });
            var b = Task.Run(() => {
                while (!started) Thread.SpinWait(1);
                for (int i = 0; i < runs; i++)
                {
                    var v = h.OneBigValue;
                    var ok = v.Correct();
                    if (!ok)
                    {
                        Console.WriteLine($"Error at iteration: {i}");
                        throw new Exception("Wrong value.");
                    }
                    if (i% 1_000_000 == 0)
                    {
                        Console.WriteLine($"Read iteration: {i} v = {v.A}");
                    }
                }
            });
            Task.WaitAll(a, b);
            Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");
            Console.ReadKey();
        }

        public unsafe void EnumerationTest()
        {
            var runs = 10_000_000;

            List<BodyStructHandle> list = new();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < runs; i++)
            {
                var h = BodyStructAllocator.AllocateNew();
                var mm = TransientGhostMemoryAllocator.Allocate(96);
                h.Body._ghost = mm.Ptr;
                h.Body._ghostOwner = mm.MemoryOwner;
                list.Add(h);
            }
            Console.WriteLine($"Elapsed to create handles: {sw.ElapsedMilliseconds} ms");

            var sum = 0l;
            sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var h in list)
            {
                sum += h.OneIntValue;
            }
            Console.WriteLine($"Elapsed Linear: {sw.ElapsedMilliseconds} ms");

            list = list.Shuffle().ToList();
            sum = 0l;
            sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (var h in list)
            {
                sum += h.OneIntValue;
            }
            Console.WriteLine($"Elapsed Shuffle: {sw.ElapsedMilliseconds} ms");
            Console.ReadKey();
        }
    }

}
