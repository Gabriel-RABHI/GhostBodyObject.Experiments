using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct SegmentReference
    {
        // Static instances for comparison/assignment
        public static SegmentReference Empty => new SegmentReference { Value = 0 };
        public static SegmentReference Tombstone => new SegmentReference { Value = ulong.MaxValue };

        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        public uint SegmentId;

        [FieldOffset(4)]
        public uint Offset;

        [FieldOffset(0)]
        public ulong Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty() => Value == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTombstone() => Value == ulong.MaxValue;

        // Helper to check if the slot has valid data (neither empty nor dead)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => Value != 0 && Value != ulong.MaxValue;
    }
}
