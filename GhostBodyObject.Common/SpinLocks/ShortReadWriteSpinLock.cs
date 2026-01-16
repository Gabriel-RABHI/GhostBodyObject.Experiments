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

namespace GhostBodyObject.Common.SpinLocks
{
    /// <summary>
    /// Standalone reader-writer lock primitive for narrow critical sections.
    /// </summary>
    /// <remarks>
    /// Supports multiple concurrent readers and exclusive writers.
    /// </remarks>
    public struct ShortReadWriteSpinLock
    {
        private const int MAX_READERS = int.MaxValue / 2;

        private int _count;

        /// <summary>
        /// Acquires the lock in read mode. Blocks if a writer holds the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnterRead()
        {
            if (Interlocked.Increment(ref _count) < 0)
            {
                Interlocked.Decrement(ref _count);
                var spinner = new SpinWait();
                while (Interlocked.Increment(ref _count) < 0)
                {
                    Interlocked.Decrement(ref _count);
                    spinner.SpinOnce();
                }
            }
        }

        /// <summary>
        /// Acquires the lock in write mode. Blocks if any readers or writers hold the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnterWrite()
        {
            if (Interlocked.CompareExchange(ref _count, -MAX_READERS, 0) != 0)
            {
                var spinner = new SpinWait();
                while (Interlocked.CompareExchange(ref _count, -MAX_READERS, 0) != 0)
                {
                    spinner.SpinOnce();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the lock is completely free (no readers or writers).
        /// </summary>
        public bool ReadFree => _count == 0;

        /// <summary>
        /// Gets a value indicating whether the lock is completely free (no readers or writers).
        /// </summary>
        public bool WriteFree => _count == 0;

        /// <summary>
        /// Releases the read lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitRead() => Interlocked.Decrement(ref _count);

        /// <summary>
        /// Releases the write lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitWrite() => Interlocked.Add(ref _count, MAX_READERS);
    }
}
