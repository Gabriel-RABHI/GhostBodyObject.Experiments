using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public unsafe struct ArrayMapSmallEntry
    {
        // Constants
        private const ushort ValueSizeMask = 0x1F;
        private const ushort ArrayLengthMask = 0x7FF;

        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        private ushort _lowerHalf;  // ValueSize (5) + ArrayLength (11)
        [FieldOffset(2)]
        public ushort ArrayOffset;  // Direct UInt16 access

        // -----------------------------------------------------------------
        // PROPERTIES
        // -----------------------------------------------------------------

        public uint ValueSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)(_lowerHalf & ValueSizeMask);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lowerHalf = (ushort)((_lowerHalf & ~ValueSizeMask) | (value & ValueSizeMask));
        }

        public uint ArrayLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)((_lowerHalf >> 5) & ArrayLengthMask);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lowerHalf = (ushort)((_lowerHalf & ~(ArrayLengthMask << 5)) | ((value & ArrayLengthMask) << 5));
        }

        // -----------------------------------------------------------------
        // COMPUTED PROPERTIES (New)
        // -----------------------------------------------------------------

        public int PhysicalSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)((_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask));
        }

        public int ArrayEndOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // ValueSize * ArrayLength
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return ArrayOffset + size;
            }
        }

        public int ArrayEndIntPaddedOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return (ArrayOffset + size + 3) & ~3;
            }
        }

        public int ArrayEndLongPaddedOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return (ArrayOffset + size + 7) & ~7;
            }
        }
    }
}
