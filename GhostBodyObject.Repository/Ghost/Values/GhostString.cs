using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    public ref struct GhostString
    {
        private readonly IEntityBody _body;
        private readonly PinnedMemory<byte> _data;
        private readonly int _arrayIndex;

        public int Length => _data.Length;

        public GhostString(IEntityBody _body, int _arrayIndex, PinnedMemory<byte> data)
        {
            _data = data;
        }

        public void SetString(string value)
        {
            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(value.AsSpan());

            // call 
        }
    }
}
