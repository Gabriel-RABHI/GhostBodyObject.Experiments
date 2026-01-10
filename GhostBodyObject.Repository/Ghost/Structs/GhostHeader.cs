using GhostBodyObject.Repository.Ghost.Constants;
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
        public GhostStatus Status;

        [FieldOffset(27)]
        public byte Flags;

        [FieldOffset(28)]
        public int MutationCounter;

        // ---------------------------------------------------------
        // Zero Fields
        // ---------------------------------------------------------
        [FieldOffset(32)]
        public long White;

        public void Initialize(ushort modelVersion)
        {
            Id = default;
            TxnId = 0;
            White = 0;
            ModelVersion = modelVersion;
            Status = GhostStatus.Standalone;
            Flags = 0x00;
            MutationCounter = 0;
        }
    }
}
