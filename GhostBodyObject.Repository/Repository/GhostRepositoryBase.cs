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

using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Helpers;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Structs;
using GhostBodyObject.Repository.Repository.Transaction;
using System.Collections.Specialized;

namespace GhostBodyObject.Repository.Repository
{
    public class GhostRepositoryBase
    {
        private object _locker = new object();

        public delegate void TransactionCommiter(ref StoreTransactionWriter writer);

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

        public RepositoryGhostIndex<MemorySegmentStore> GhostIndex => _ghostIndex;

        public MemorySegmentStore Store => _store;
        #endregion

        public void CommitTransaction<T>(T commiter, bool twoStage = false)
            where T : IModifiedBodyStream
        {
            if (twoStage)
            {
                // -------- Split this process in two steps -------- //
                // 1. Compute the necessary space, reserve it in the store.
                List<BodyBase> modifiedBodies = new List<BodyBase>();
                lock (_locker)
                {
                    commiter.ReadModifiedBodies((body) => modifiedBodies.Add(body));
                    foreach (var body in modifiedBodies)
                    {
                        // reserve space int the store
                    }
                }
                // 2. Write the transaction in the reserved store memory.
                //    Concurrently, an another thread can enter the critical section to reserve Store space.
                if (modifiedBodies.Count > 0)
                {
                    var writer = new StoreTransactionWriter()
                    {
                        Repository = this,
                        Store = _store,
                        TransactionId = _transactionRange.CurrentTransactionId
                    };
                    writer.OpenTransaction();
                    foreach (var body in modifiedBodies)
                        writer.StoreGhost(body._data);
                    writer.CloseTransaction();
                    _transactionRange.IncrementCurrentTransactionId();
                }
            }
            else
            {
                // -------- Single stage commit -------- //
                lock (_locker)
                {
                    List<BodyBase> modifiedBodies = new List<BodyBase>();
                    commiter.ReadModifiedBodies((body) => modifiedBodies.Add(body));
                    if (modifiedBodies.Count > 0)
                    {
                        var writer = new StoreTransactionWriter()
                        {
                            Repository = this,
                            Store = _store,
                            TransactionId = _transactionRange.CurrentTransactionId
                        };
                        writer.OpenTransaction();
                        foreach (var body in modifiedBodies)
                            writer.StoreGhost(body._data);
                        writer.CloseTransaction();
                        _transactionRange.IncrementCurrentTransactionId();
                    }
                }
            }
                
        }

        public GhostRepositoryBase(SegmentStoreMode mode = SegmentStoreMode.InMemoryRepository, string path = default)
        {
            _store = new MemorySegmentStore(mode);
            _ghostIndex = new RepositoryGhostIndex<MemorySegmentStore>(_store);
        }

        /// <summary>
        /// When a transaction is opened, his _openingTxnId field is the view generation of the ghost repository.
        /// The 
        /// </summary>
        /// <param name="tnx"></param>
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
    }

    public struct StoreTransactionWriter
    {
        public GhostRepositoryBase Repository { get; init; }

        public MemorySegmentStore Store { get; init; }

        public long TransactionId { get; init; }

        public void OpenTransaction()
        {
        }

        public void CloseTransaction()
        {
        }

        public SegmentReference StoreGhost(PinnedMemory<byte> ghost)
        {
            var r = Store.StoreGhost(ghost, TransactionId);
            Repository.GhostIndex.AddGhost(r);
            return r;
        }
    }
}
