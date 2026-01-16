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
    /// Standalone recursive lock primitive for narrow critical sections.
    /// </summary>
    public struct ShortRecursiveSpinLock
    {
        private int _count;
        private int _thId;

        /// <summary>
        /// Acquires the lock. Blocks the current thread (spinning) until the lock is acquired.
        /// </summary>
        /// <remarks>
        /// If the lock is already held by the current thread, the recursion count is incremented.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            if (Volatile.Read(ref _thId) != id)
            {
                if (Interlocked.CompareExchange(ref _thId, id, 0) != 0)
                {
                    var spinner = new SpinWait();
                    while (Interlocked.CompareExchange(ref _thId, id, 0) != 0)
                        spinner.SpinOnce();
                }
            }
            _count++;
        }

        /// <summary>
        /// Attempts to acquire the lock immediately.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was acquired; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            if (_thId == id)
                return true;
            return Interlocked.CompareExchange(ref _thId, id, 0) == 0;
        }

        /// <summary>
        /// Gets a value indicating whether the lock is free.
        /// </summary>
        public bool Free => Volatile.Read(ref _thId) == 0;

        /// <summary>
        /// Gets a value indicating whether the lock is currently held.
        /// </summary>
        public bool Busy => !Free;

        /// <summary>
        /// Releases the lock.
        /// </summary>
        /// <remarks>
        /// Decrements the recursion count. The lock is released only when the count reaches zero.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit()
        {
            if (--_count == 0)
                Volatile.Write(ref _thId, 0);
        }
    }
}
