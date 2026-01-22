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

namespace GhostBodyObject.Repository.Repository.Helpers
{
    /// <summary>
    /// This class manage the transaction id range for a Ghost Repository.
    /// Each time a new transaction is opened, the CurrentTransactionId is assigned to the transaction and then incremented.
    /// For the lifetime of a transaction, the repository must retain the memory segments that are valid for the transaction's view.
    /// When the transaction is closed, the view counter for the transaction id is decremented.
    /// If dropping to 0, it is a signal that the repository's Store do not have to retain the MemorySegment for that transaction id.
    /// </summary>
    public class GhostRepositoryTransactionIdRange
    {
        private readonly object _lock = new object();
        private long _topTxnId = 0;
        private readonly SortedList<long, int> _views = new SortedList<long, int>();

        public long BottomTransactionId {
            get {
                lock (_lock)
                {
                    if (_views.Count == 0)
                        return Interlocked.Read(ref _topTxnId);
                    return _views.Keys[0];
                }
            }
        }

        public long TopTransactionId => Interlocked.Read(ref _topTxnId);

        /// <summary>
        /// Increments the current transaction identifier and returns the updated value.
        /// </summary>
        /// <returns>The new transaction identifier after incrementing the current value.</returns>
        public long IncrementTopTransactionId()
        {
            lock (_lock)
                return ++_topTxnId;
        }

        /// <summary>
        /// Increment the view counter for the specific transaction id.
        /// </summary>
        /// <param name="txnId">The transaction id for wich a new viewer is registered.</param>
        public long AddTransactionViewer()
        {
            lock (_lock)
            {
                var txnId = _topTxnId;
                if (_views.TryGetValue(txnId, out int count))
                {
                    _views[txnId] = count + 1;
                } else
                {
                    _views.Add(txnId, 1);
                }
                return _topTxnId;
            }
        }

        /// <summary>
        /// Decerment the view counter for the specific transaction id.
        /// </summary>
        /// <param name="txnId"></param>
        /// <returns>True if the viewer counter drops to 0. It is a signal that the repository's Store do not have to retain the MemorySegment.</returns>
        public bool RemoveTransactionViewer(long txnId)
        {
            lock (_lock)
            {
                if (_views.TryGetValue(txnId, out int count))
                {
                    if (count <= 1)
                    {
                        _views.Remove(txnId);
                        return true;
                    } else
                    {
                        _views[txnId] = count - 1;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
