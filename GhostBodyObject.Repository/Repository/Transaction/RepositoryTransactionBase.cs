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
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Segment;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    public abstract class RepositoryTransactionBase
    {
        private readonly GhostRepositoryBase _repository;
        private readonly bool _isReadOnly;
        private long _openingTxnId;
        private MemorySegmentStoreHolders _holders;
        protected RepositoryTransactionBodyIndex _bodyIndex;

        public volatile bool IsBusy;

        public RepositoryTransactionBase(GhostRepositoryBase repository, bool isReadOnly, ushort maxTypeIdentifier)
        {
            _repository = repository;
            _isReadOnly = isReadOnly;
            _holders = repository.Store.GetHolders();
            _openingTxnId = repository.CurrentTransactionId;
            _bodyIndex = new RepositoryTransactionBodyIndex(this, 1024);
        }

        public GhostRepositoryBase Repository => _repository;

        public bool IsReadOnly => _isReadOnly;

        public bool NeedReborn => false; // throw new NotImplementedException();

        public long OpeningTxnId => _openingTxnId;

        public RepositoryTransactionBodyIndex BodyIndex => _bodyIndex;

        public void RegisterBody<TBody>(TBody body)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            var map = _bodyIndex.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
            if (body.Inserted)
                map.InsertedIds.Add(body.Id);
            if (body.MappedDeleted || body.MappedModified)
                map.MappedMutedIds.Add(body.Id);
            map.Set(body);
        }

        public void RemoveBody<TBody>(TBody body)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            var map = _bodyIndex.GetBodyMap<TBody>(TBody.GetTypeIdentifier());
            if (map != null)
            {
                map.Remove(body.Id);
                map.InsertedIds.Remove(body.Id);
            }
        }
    }
}
