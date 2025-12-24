using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
    public sealed unsafe class GhostHolder
    {
        [FieldOffset(0)]
        public PinnedMemory<byte> _data;

        [FieldOffset(24)]
        public VectorTable* _vTable;
    }
}
