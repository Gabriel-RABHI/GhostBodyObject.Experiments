using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{


    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct VectorTable
    {
        [FieldOffset(0)]
        public short TypeIdentifier;

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
        public delegate*<Customer, Memory<byte>, int, void> SwapAnyArray;
    }
}
