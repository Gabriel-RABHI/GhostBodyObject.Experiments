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
using GhostBodyObject.Repository.Repository.Segment;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    public abstract class RepositoryTransactionBase
    {
        private readonly GhostRepositoryBase _repository;
        private readonly bool _isReadOnly;
        private readonly long _openingTxnId;
        private readonly MemorySegmentStoreHolders _holders;
        protected RepositoryTransactionBodyIndex _bodyIndex;
        private bool _closed;

        public volatile bool IsBusy;

        public RepositoryTransactionBase(GhostRepositoryBase repository, bool isReadOnly, ushort maxTypeIdentifier)
        {
            _repository = repository;
            _isReadOnly = isReadOnly;
            _holders = repository.Store.GetHolders();
            _openingTxnId = repository.GetNewTxnId();
            _bodyIndex = new RepositoryTransactionBodyIndex(this, maxTypeIdentifier);
        }

        public void Close()
        {
            if (!_closed)
            {
                _repository.Forget(_openingTxnId);
                _closed = true;
                _bodyIndex.Release();
            }
        }

        public GhostRepositoryBase Repository => _repository;

        public bool IsReadOnly => _isReadOnly;

        public bool NeedReborn => false; // throw new NotImplementedException();

        public long OpeningTxnId => _openingTxnId;

        public void RegisterBody<TBody>(TBody body)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            var map = _bodyIndex.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
            if (body.Inserted || body.MappedDeleted || body.MappedModified)
            {
                _bodyIndex.RecordModifiedBody(body);
                map.RecordModifiedBody(body);
            }
            map.Set(body);
        }

        public void RemoveBody<TBody>(TBody body)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            var map = _bodyIndex.GetBodyMap<TBody>(TBody.GetTypeIdentifier());
            if (map != null)
            {
                map.Remove(body.Id);
                map.RemoveModifiedBody(body);
            }
        }
    }
}
