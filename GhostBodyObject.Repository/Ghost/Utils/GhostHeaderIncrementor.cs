using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    /// <summary>
    /// Provides functionality for incrementally calculating memory offsets for ghost header structures, supporting
    /// type-based size advancement and alignment padding.
    /// </summary>
    /// <remarks>This class is typically used when constructing or serializing binary data structures that
    /// require precise control over field offsets and alignment. Offsets are advanced based on the size of types or by
    /// applying specific padding to meet alignment requirements. The initial offset is set to the size of the ghost
    /// header structure.</remarks>
    public class GhostHeaderIncrementor
    {
        private int _offset = GhostHeader.SIZE;

        public int Push<T>()
        {
            int currentOffset = _offset;
            _offset += Unsafe.SizeOf<T>();
            return currentOffset;
        }

        public int Padd(int padding)
        {
            if(padding != 2 && padding != 4 && padding != 8 && padding != 16)
                throw new System.ArgumentException("Padding must be 2, 4, 8, or 16 bytes.");
            _redo:
            if ((_offset % padding) != 0)
            {
                _offset++;
                goto _redo;
            }
            return _offset;
        }

        public int Offset => _offset;
    }
}
