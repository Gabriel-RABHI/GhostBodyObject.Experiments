using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Vectors
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct VectorTableHeader
    {
        [FieldOffset(0)]
        public ushort TypeIdentifier;

        [FieldOffset(2)]
        public short ModelVersion;

        [FieldOffset(4)]
        public bool ReadOnly;

        [FieldOffset(5)]
        public bool Large;

        [FieldOffset(8)]
        public int ArrayMapOffset;

        [FieldOffset(12)]
        public int ArrayMapLength;

        [FieldOffset(16)]
        public int MinimalGhostSize;
    }
}
