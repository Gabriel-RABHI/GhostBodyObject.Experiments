using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
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
        public long TxnId;

        [FieldOffset(24)]
        public ushort ModelVersion;

        [FieldOffset(26)]
        public ushort Flags;

        [FieldOffset(28)]
        public int MutationCounter;

        // ---------------------------------------------------------
        // Zero Fields
        // ---------------------------------------------------------
        [FieldOffset(32)]
        public long White;
    }
}
