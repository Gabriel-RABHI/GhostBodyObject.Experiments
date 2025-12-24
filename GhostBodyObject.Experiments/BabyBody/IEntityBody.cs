using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    public interface IEntityBody
    {
    }

    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
    public class BodyUnion : IEntityBody
    {
        [FieldOffset(0)]
        public GhostContext _context;

        [FieldOffset(8)]
        public IntPtr _vTablePtr;

        [FieldOffset(16)]
        public PinnedMemory<byte> _data;
    }
}
