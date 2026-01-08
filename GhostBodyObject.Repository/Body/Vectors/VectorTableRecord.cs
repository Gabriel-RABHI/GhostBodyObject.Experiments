using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Vectors
{
    // ---------------------------------------------------------
    // GENERIC
    // ---------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VectorTableRecord
    {
        public PinnedMemory<byte> InitialGhost;

        public VectorTableHeader* Standalone;

        public VectorTableHeader* MappedReadOnly;

        public VectorTableHeader* MappedMutable;

        public int GhostSize;
    }
}
