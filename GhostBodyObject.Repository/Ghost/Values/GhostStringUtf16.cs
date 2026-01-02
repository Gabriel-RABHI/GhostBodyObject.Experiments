using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    public ref struct GhostStringUtf16
    {
        private readonly IEntityBody _body;
        private readonly PinnedMemory<byte> _data;
        private readonly int _arrayIndex;

        public int Length => _data.Length;

        public GhostStringUtf16(IEntityBody _body, int _arrayIndex, PinnedMemory<byte> data)
        {
            _data = data;
        }

        public unsafe void SetString(string value)
        {
            var union = Unsafe.As<BodyUnion>(_body);
            ((VectorTableHeader*)union._vTablePtr)->SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _arrayIndex);
        }
    }
}
