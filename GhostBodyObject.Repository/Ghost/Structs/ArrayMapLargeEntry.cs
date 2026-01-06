using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct ArrayMapLargeEntry
    {
        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        public byte ValueSize;      // Direct Byte access
        [FieldOffset(4)]
        public uint ArrayOffset;    // Direct UInt32 access
        [FieldOffset(0)]
        private uint _lowerHalf;    // For accessing ArrayLength (top 24 bits)

        // -----------------------------------------------------------------
        // PROPERTIES
        // -----------------------------------------------------------------

        public uint ArrayLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _lowerHalf >> 8;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lowerHalf = (_lowerHalf & 0xFF) | (value << 8);
        }

        // -----------------------------------------------------------------
        // COMPUTED PROPERTIES (New)
        // -----------------------------------------------------------------

        /// <summary>
        /// Total size in bytes (ValueSize * ArrayLength).
        /// </summary>
        public int PhysicalSize
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(ValueSize * (_lowerHalf >> 8));
        }

        /// <summary>
        /// The absolute byte offset where this array ends.
        /// </summary>
        public int ArrayEndOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(ArrayOffset + (ValueSize * (_lowerHalf >> 8)));
        }

        /// <summary>
        /// The end offset padded to the next 4-byte boundary.
        /// </summary>
        public int ArrayEndIntPaddedOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Calculation: (EndOffset + 3) & ~3
                long end = (long)ArrayOffset + (ValueSize * (_lowerHalf >> 8));
                return (int)((end + 3) & ~3);
            }
        }

        /// <summary>
        /// The end offset padded to the next 8-byte boundary.
        /// </summary>
        public int ArrayEndLongPaddedOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Calculation: (EndOffset + 7) & ~7
                long end = (long)ArrayOffset + (ValueSize * (_lowerHalf >> 8));
                return (int)((end + 7) & ~7);
            }
        }
    }
}
