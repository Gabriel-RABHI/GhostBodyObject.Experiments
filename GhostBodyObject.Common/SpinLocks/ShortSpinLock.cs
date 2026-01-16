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
    /// Standalone lock primitive for narrow critical sections.
    /// </summary>
    /// <remarks>
    /// This synchronysation primitive is not recursive : the same thread cannot enter multiple times.
    /// </remarks>
    public struct ShortSpinLock
    {
        private volatile int _lock;

        /// <summary>
        /// Acquires the lock. Blocks the current thread (spinning) until the lock is acquired.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter()
        {
            if (Interlocked.CompareExchange(ref _lock, 1, 0) == 0)
                return;
            EnterSlow();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void EnterSlow()
        {
            var spinner = new SpinWait();
            do
            {
                while (_lock != 0)
                    spinner.SpinOnce();
            } while (Interlocked.CompareExchange(ref _lock, 1, 0) != 0);
        }

        /// <summary>
        /// Attempts to acquire the lock immediately.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was acquired; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnter() => Interlocked.CompareExchange(ref _lock, 1, 0) == 0;

        /// <summary>
        /// Gets a value indicating whether the lock is free.
        /// </summary>
        public bool Free => _lock == 0;

        /// <summary>
        /// Releases the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit() => _lock = 0;
    }
}
