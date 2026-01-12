using GhostBodyObject.Repository.Repository.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct StoreTransactionContinuation
    {
        [FieldOffset(0)]
        public SegmentStructureType T;

        [FieldOffset(1)]
        public byte Type;

        [FieldOffset(2)]
        public ushort Empty;

        [FieldOffset(4)]
        public uint PreviousSegmentId;

        [FieldOffset(8)]
        public ulong Id;
    }
}
