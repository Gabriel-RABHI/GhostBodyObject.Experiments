using GhostBodyObject.Common.Objects;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
    public unsafe struct GhostHeader
    {
        public const int SIZE = 40;
        public const int WHITE_OFFSET = 32;

        // ---------------------------------------------------------
        // Standard Fields
        // ---------------------------------------------------------
        [FieldOffset(0)]
        public GhostId Id;

        [FieldOffset(16)]
        public ushort ModelVersion;

        [FieldOffset(18)]
        public ushort Flags;

        [FieldOffset(20)]
        public int MutationCounter;

        [FieldOffset(24)]
        public int TxnId;

        // ---------------------------------------------------------
        // Zero Fields
        // ---------------------------------------------------------
        [FieldOffset(32)]
        public long White;
    }
}
