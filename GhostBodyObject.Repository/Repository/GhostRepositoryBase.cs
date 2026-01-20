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

using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Helpers;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction;

namespace GhostBodyObject.Repository.Repository
{
    public class GhostRepositoryBase : IDisposable
    {
        private object _locker = new object();
        private ShortSpinLock _spinLocker = new ShortSpinLock();

        /// <summary>
        /// The underlying Memory Segment Store where Ghost data is stored.
        /// </summary>
        private readonly MemorySegmentStore _store;

        /// <summary>
        /// The ghost index that reference memory segment.
        /// </summary>
        private readonly RepositoryGhostIndex<MemorySegmentStore> _ghostIndex;

        /// <summary>
        /// Represents the range of transaction IDs associated with the current repository transaction.
        /// </summary>
        private GhostRepositoryTransactionIdRange _transactionRange = new GhostRepositoryTransactionIdRange();

        #region PROPERTIES
        public long CurrentTransactionId => _transactionRange.CurrentTransactionId;

        public long BottomTransactionId => _transactionRange.BottomTransactionId;

        public RepositoryGhostIndex<MemorySegmentStore> GhostIndex => _ghostIndex;

        public MemorySegmentStore Store => _store;

        ~GhostRepositoryBase()
        {
            Dispose();
        }

        public GhostRepositoryBase(SegmentStoreMode mode = SegmentStoreMode.InMemoryVolatileRepository, string path = default)
        {
            _store = new MemorySegmentStore(mode, path);
            _ghostIndex = new RepositoryGhostIndex<MemorySegmentStore>(_store);
        }
        #endregion

        public void CommitTransaction<T>(T commiter, bool twoStage = false)
            where T : IModifiedBodyStream
        {
            lock (_locker)
            {
                if (_store.WriteTransaction(commiter, _transactionRange.CurrentTransactionId, (id, r) =>
                {
                    _ghostIndex.AddGhost(BottomTransactionId, r);
                }))
                {
                    _transactionRange.IncrementCurrentTransactionId();
                }
            }
        }

        public void Retain(RepositoryTransactionBase tnx)
        {
            _transactionRange.IncrementTransactionViewId(tnx.OpeningTxnId);
        }

        public void Forget(RepositoryTransactionBase tnx)
        {
            if (_transactionRange.DecrementTransactionViewId(tnx.OpeningTxnId))
            {
                Store.UpdateHolders(_transactionRange.BottomTransactionId, _transactionRange.TopTransactionId);
            }
        }

        public void Dispose()
        {
            _store.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
