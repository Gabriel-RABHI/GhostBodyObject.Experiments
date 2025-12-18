using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Utilities
{
    /// <summary>
    /// High-performance, unmanaged Pseudo-Random Number Generator.
    /// Using ThreadStatic to avoid lock contention.
    /// </summary>
    internal static class XorShift64
    {
        [ThreadStatic]
        private static ulong _state;

        /// <summary>
        /// Generates the next pseudo-random 64-bit unsigned integer using a thread-local Xorshift algorithm.
        /// </summary>
        /// <remarks>This method is thread-safe and maintains a separate random state for each thread. The
        /// sequence is not cryptographically secure and should not be used for security-sensitive purposes. The initial
        /// state is seeded using a combination of system tick count and the current managed thread ID if
        /// uninitialized.</remarks>
        /// <returns>A 64-bit unsigned integer representing the next value in the pseudo-random sequence.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Next()
        {
            ulong x = _state;
            if (x == 0)
            {
                x = (ulong)Environment.TickCount64 ^ (ulong)Environment.CurrentManagedThreadId;
                // Fallback if the XOR resulted in 0
                if (x == 0)
                    x = 0xCAFEB4BE_DEADB8EF;
            }
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            _state = x;
            return x;
        }
    }
}
