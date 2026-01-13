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

#define PER_THREAD

using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Memory
{
    /// <summary>
    /// Provides thread-local memory allocation from a shared, pinned buffer for efficient interop and temporary data
    /// scenarios.
    /// </summary>
    /// <remarks>Allocations are performed from a buffer that is pinned in memory, which can improve
    /// performance when passing memory to unmanaged code. Each thread maintains its own buffer, and allocations are
    /// reused until the buffer's capacity is exceeded. This class is intended for scenarios where fast, temporary
    /// allocations are needed and the lifetime of the allocated memory is short. Allocated memory is not automatically
    /// cleared or released until the buffer is replaced.</remarks>
    public static class ManagedArenaAllocator
    {
        public const int DefaultPageSize = 128 * 1024;

        [ThreadStatic]
        private static byte[]? _buffer;
        [ThreadStatic]
        private static int _offset;

        /// <summary>
        /// Allocates a block of memory of the specified size from a shared, pinned buffer. The memory is released when no longer referenced.
        /// </summary>
        /// <remarks>The returned memory is backed by a pinned buffer, which may improve performance for
        /// interop scenarios. Subsequent allocations may reuse the same buffer until its capacity is
        /// exceeded.</remarks>
        /// <param name="size">The number of bytes to allocate. Must be less than or equal to the default page size.</param>
        /// <returns>A Memory<byte> instance representing the allocated block of memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified size exceeds the default page size.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PinnedMemory<byte> Allocate(int size)
        {
            if ((uint)size > DefaultPageSize)
                throw new ArgumentOutOfRangeException(nameof(size));

            byte[]? buffer = _buffer;
            int offset = _offset;

            if (buffer == null || (uint)(offset + size) > (uint)buffer.Length)
            {
                buffer = GC.AllocateUninitializedArray<byte>(DefaultPageSize, pinned: true);
                _buffer = buffer;
                offset = 0;
            }
            _offset = offset + size;

            return new PinnedMemory<byte>(buffer, offset, size);
        }
    }
}
