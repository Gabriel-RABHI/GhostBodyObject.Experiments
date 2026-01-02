using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Vectors
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct VectorTableHeader
    {
        [FieldOffset(0)]
        public ushort TypeCombo;

        [FieldOffset(2)]
        public short ModelVersion;

        [FieldOffset(4)]
        public bool ReadOnly;

        [FieldOffset(5)]
        public bool LargeArrays;

        [FieldOffset(8)]
        public int MinimalGhostSize;

        [FieldOffset(12)]
        public int ArrayMapOffset;

        [FieldOffset(16)]
        public int ArrayMapLength;

        [FieldOffset(20)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, void> SwapAnyArray;
    }
}
