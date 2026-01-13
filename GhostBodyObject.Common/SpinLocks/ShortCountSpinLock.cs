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
    /// Standalone lock primitive that accept a limited entrancy count.
    /// <remarks>
    /// This synchronysation primitive is not recursive : the same thread cannot enter multiple times.
    /// </remarks>
    /// </summary>
    public struct ShortCountSpinLock
    {
        private int _count;
        private volatile int _max;

        /// <summary>
        /// Construct the primitive by specifying the number of enters allowed.
        /// </summary>
        /// <param name="max">Number of enters allowed</param>
        public ShortCountSpinLock(int max)
        {
            _count = 0;
            _max = max;
        }

        /// <summary>
        /// Enter the lock if the entering count is not reached.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enter()
        {
            if (Interlocked.Increment(ref _count) > _max)
            {
                Interlocked.Decrement(ref _count);
                var spinner = new SpinWait();
                while (Interlocked.Increment(ref _count) > _max)
                {
                    Interlocked.Decrement(ref _count);
                    spinner.SpinOnce();
                }
            }
        }

        /// <summary>
        /// Return true if the maximum entering count is not reached.
        /// </summary>
        public bool Free => _count < _max;

        /// <summary>
        /// Release the lock.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exit() => Interlocked.Decrement(ref _count);
    }
}
