using GhostBodyObject.Repository.Repository.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct SealedSegmentFooter
    {
        [FieldOffset(0)]
        public SegmentStructureType T;

        [FieldOffset(1)]
        public byte Mode;

        [FieldOffset(2)]
        public ushort Empty;

        [FieldOffset(4)]
        public uint SegmentEndOffset;
    }
}
