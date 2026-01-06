using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Vectors
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct VectorTableHeader
    {
        [FieldOffset(0)]
        public ushort TypeCombo;

        [FieldOffset(2)]
        public short ModelVersion;

        [FieldOffset(4)]
        public bool ReadOnly;

        [FieldOffset(5)]
        public bool LargeArrays;

        [FieldOffset(8)]
        public int MinimalGhostSize;

        [FieldOffset(12)]
        public int ArrayMapOffset;

        [FieldOffset(16)]
        public int ArrayMapLength;

        /// <summary>
        /// Replaces the entire contents of the array at the specified index.
        /// </summary>
        [FieldOffset(20)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, void> SwapAnyArray;

        /// <summary>
        /// Appends data to the end of the array at the specified index.
        /// </summary>
        [FieldOffset(28)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, void> AppendToArray;

        /// <summary>
        /// Prepends data to the beginning of the array at the specified index.
        /// </summary>
        [FieldOffset(36)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, void> PrependToArray;

        /// <summary>
        /// Inserts data at the specified byte offset within the array at the specified index.
        /// Parameters: body, data, arrayIndex, byteOffset
        /// </summary>
        [FieldOffset(44)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, int, void> InsertIntoArray;

        /// <summary>
        /// Removes data from the array at the specified index.
        /// Parameters: body, arrayIndex, byteOffset, byteLength
        /// </summary>
        [FieldOffset(52)]
        public delegate*<BodyUnion, int, int, int, void> RemoveFromArray;

        /// <summary>
        /// Replaces a range within the array with new data.
        /// Parameters: body, replacementData, arrayIndex, byteOffset, byteLengthToRemove
        /// </summary>
        [FieldOffset(60)]
        public delegate*<BodyUnion, ReadOnlySpan<byte>, int, int, int, void> ReplaceInArray;
    }
}
