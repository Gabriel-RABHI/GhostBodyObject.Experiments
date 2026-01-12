using GhostBodyObject.Repository.Repository.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct SegmentHeader
    {
        [FieldOffset(0)]
        public SegmentStructureType T;

        [FieldOffset(1)]
        public SegmentStoreMode Mode;

        [FieldOffset(2)]
        public ushort Empty;

        [FieldOffset(4)]
        public int SegmentId;

        [FieldOffset(8)]
        public int Capacity;

        [FieldOffset(12)]
        public int HeadPosition;

        public static SegmentHeader Create(SegmentStoreMode mode, int segmentId, int capacity)
        {
            return new SegmentHeader
            {
                T = SegmentStructureType.SegmentHeader,
                Mode = mode,
                Empty = 0,
                SegmentId = segmentId,
                Capacity = capacity,
                HeadPosition = 0
            };
        }
    }
}
