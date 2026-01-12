using GhostBodyObject.Repository.Repository.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public unsafe struct StoreTransactionHeader
    {
        [FieldOffset(0)]
        public SegmentStructureType T;

        [FieldOffset(1)]
        public SegmentTransactionOrigin Origin;

        [FieldOffset(2)]
        public ushort Empty;

        [FieldOffset(4)]
        public uint PreviousOffset;

        [FieldOffset(8)]
        public uint PreviousSegmentId;

        [FieldOffset(12)]
        public uint Size;

        [FieldOffset(16)]
        public ulong Id;

        [FieldOffset(24)]
        public ulong CheckSum;
    }
}
