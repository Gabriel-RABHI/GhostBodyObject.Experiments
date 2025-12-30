using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    public class GhostHeaderIncrementer
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
