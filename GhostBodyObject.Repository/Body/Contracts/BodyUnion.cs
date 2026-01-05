using GhostBodyObject.Repository.Body.Vectors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Contracts
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 42)]
    public class BodyUnion : IEntityBody
    {
        [FieldOffset(0)]
        public object _context;

        [FieldOffset(8)]
        public IntPtr _vTablePtr;

        [FieldOffset(16)]
        public PinnedMemory<byte> _data;

        [FieldOffset(40)]
        protected bool _mapped;

        [FieldOffset(41)]
        protected bool _immutable;

        internal unsafe VectorTableHeader* _vTableHeader
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (VectorTableHeader*)_vTablePtr;
        }
    }
}
