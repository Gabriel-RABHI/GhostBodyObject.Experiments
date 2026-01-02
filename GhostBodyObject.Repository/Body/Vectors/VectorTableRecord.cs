using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{
    // ---------------------------------------------------------
    // GENERIC
    // ---------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VectorTableRecord
    {
        public int GhostSize;

        public PinnedMemory<byte> InitialGhost;

        public VectorTableHeader* Initial;

        public VectorTableHeader* Standalone;

        public VectorTableHeader* MappedReadOnly;

        public VectorTableHeader* MappedMutable;
    }
}
