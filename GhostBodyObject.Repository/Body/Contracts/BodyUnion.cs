using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Contracts
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
    public class BodyUnion : IEntityBody
    {
        [FieldOffset(0)]
        public object _context;

        [FieldOffset(8)]
        public IntPtr _vTablePtr;

        [FieldOffset(16)]
        public PinnedMemory<byte> _data;
    }
}
