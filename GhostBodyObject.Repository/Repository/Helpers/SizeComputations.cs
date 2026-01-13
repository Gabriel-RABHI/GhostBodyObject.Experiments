using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public static class GetSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Of(PinnedMemory<byte> ghost) => ghost.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSizeAlignedTo8Bytes(PinnedMemory<byte> ghost)
        {
            return (ghost.Length + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T>(int variablePart = 0)
            where T : unmanaged
        {
            return ((sizeof(T) + variablePart) + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T1, T2>(int variablePart = 0)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            return ((sizeof(T1) + sizeof(T2) + variablePart) + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T1, T2, T3>(int variablePart = 0)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            return ((sizeof(T1) + sizeof(T2) + sizeof(T3) + variablePart) + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T1, T2, T3, T4>(int variablePart = 0)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            return ((sizeof(T1) + sizeof(T2) + sizeof(T3) + sizeof(T4) + variablePart) + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T1, T2, T3, T4, T5>(int variablePart = 0)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
        {
            return ((sizeof(T1) + sizeof(T2) + sizeof(T3) + sizeof(T4) + sizeof(T5) + variablePart) + 7) & ~7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int Of8Aligned<T1, T2, T3, T4, T5, T6>(int variablePart = 0)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
        {
            return ((sizeof(T1) + sizeof(T2) + sizeof(T3) + sizeof(T4) + sizeof(T5) + sizeof(T6) + variablePart) + 7) & ~7;
        }
    }
}
