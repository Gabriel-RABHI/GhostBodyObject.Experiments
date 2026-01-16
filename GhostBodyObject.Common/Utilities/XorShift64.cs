/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Utilities
{
    /// <summary>
    /// High-performance, unmanaged Pseudo-Random Number Generator.
    /// Using ThreadStatic to avoid lock contention.
    /// </summary>
    public static class XorShift64
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
