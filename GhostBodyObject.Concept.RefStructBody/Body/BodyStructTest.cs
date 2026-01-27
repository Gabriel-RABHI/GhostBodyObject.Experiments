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

    public unsafe struct BodyStructHandle
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
                int offset = 8; // ((MyVTable*)vtable)->BirthDate_Offset;
                return *(LargeStruct*)(ghost + offset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                ref var _slot = ref Body;
                try
                {
                    // A. Lock Readers (Sequence = Odd)
                    Interlocked.Increment(ref _slot._sequence);
                    var mm = TransientGhostMemoryAllocator.Allocate(96);
                    _slot._ghost = mm.Ptr;
                    _slot._ghostOwner = mm.MemoryOwner;
                    int offset = 8;
                    *(LargeStruct*)(_slot._ghost + offset) = value;
                } finally
                {
                    // D. Unlock Readers (Sequence = Even, +2)
                    Interlocked.Increment(ref _slot._sequence);
                }
            }

            /*
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {

                var _slot = Body;
                // 1. Guard Scope (Affinity + Lock)
                _slot.GuardWrite();

                // 2. Load State
                // Optim: Check if we can write in-place without Seqlock
                bool isMutable = !IsMemoryImmutable(_slot->GhostPtr);

                if (isMutable)
                {
                    // *** FAST PATH (In-Place) ***
                    // No Seqlock increment needed. Pointers are stable.
                    // We just write the bytes directly.

                    var setter = ((MyVTable*)_slot->VTablePtr)->BirthDate_Setter;
                    setter(_slot->GhostPtr, value.Ticks);
                } else
                {
                    // *** SLOW PATH (Structural Change) ***
                    // We are moving memory (CoW). Must protect Readers.

                    try
                    {
                        // A. Lock Readers (Sequence = Odd)
                        Interlocked.Increment(ref _slot->Sequence);

                        // B. Migrate / CoW
                        // This changes _slot->GhostPtr and _slot->VTablePtr
                        _txn.EnsureWritable(_slot, 100);

                        // C. Perform Write on New Memory
                        var setter = ((MyVTable*)_slot->VTablePtr)->BirthDate_Setter;
                        setter(_slot->GhostPtr, value.Ticks);
                    } finally
                    {
                        // D. Unlock Readers (Sequence = Even, +2)
                        Interlocked.Increment(ref _slot->Sequence);
                    }
                }

                // 3. Release Scope
                _txn.ReleaseWrite();
            }
            */
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
    }

}
