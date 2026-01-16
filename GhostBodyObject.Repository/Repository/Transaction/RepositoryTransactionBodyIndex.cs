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
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Transaction.Index;

namespace GhostBodyObject.Repository.Repository.Transaction
{
    public class RepositoryTransactionBodyIndex : IModifiedBodyStream
    {
        private readonly RepositoryTransactionBase _txn;
        private object[] _maps;

        public RepositoryTransactionBodyIndex(RepositoryTransactionBase txn, ushort maxTypeIdentifier)
        {
            _txn = txn;
            _maps = new object[maxTypeIdentifier + 8];
        }

        public ShardedTransactionBodyMap<TBody> GetBodyMap<TBody>(ushort typeIdentifier)
            where TBody : BodyBase
        {
            return (ShardedTransactionBodyMap<TBody>)_maps[typeIdentifier];
        }

        public ShardedTransactionBodyMap<TBody> GetOrCreateBodyMap<TBody>(ushort typeIdentifier)
            where TBody : BodyBase
        {
            if (_maps[typeIdentifier] == null)
            {
                _maps[typeIdentifier] = new ShardedTransactionBodyMap<TBody>();
            }
            return (ShardedTransactionBodyMap<TBody>)_maps[typeIdentifier];
        }

        public void ReadModifiedBodies(Action<BodyBase> reader)
        {
            for (int i = 0; i < _maps.Length; i++)
            {
                if (_maps[i] is IModifiedBodyStream stream)
                {
                    stream.ReadModifiedBodies(reader);
                }
            }
        }

        public void ForEach<TBody>(Action<TBody> action)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            unsafe
            {
                var map = _txn.Repository.GhostIndex.GetIndex(TBody.GetTypeIdentifier(), false);
                var bodyMap = GetBodyMap<TBody>(TBody.GetTypeIdentifier());
                if (_txn.IsReadOnly)
                {
                    if (map != null)
                    {
                        if (bodyMap != null)
                        {
                            if (bodyMap.Count == 0)
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                    {
                                        var body = TBody.Create(ghost, true, true);
                                        if (bodyMap == null)
                                            bodyMap = GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                        bodyMap.Set(body);
                                        action(body);
                                    }
                                }
                            }
                            else
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    var body = bodyMap.Get(ghost.As<GhostHeader>()->Id, out var exist);
                                    if (exist)
                                    {
                                        if (body.Status != GhostStatus.MappedDeleted)
                                            action(body);
                                    }
                                    else
                                    {
                                        if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                        {
                                            body = TBody.Create(ghost, true, true);
                                            bodyMap.Set(body);
                                            action(body);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                            while (enumerator.MoveNext())
                            {
                                var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                {
                                    var body = TBody.Create(ghost, true, true);
                                    if (bodyMap == null)
                                        bodyMap = GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    bodyMap.Set(body);
                                    action(body);
                                }
                            }
                        }
                    }
                    else
                    {
                        // -------- No Map of this type -------- //
                        // Because the transaction is read-only, there is no body map to consider.
                    }
                }
                else
                {
                    if (map != null)
                    {
                        if (bodyMap != null)
                        {
                            if (bodyMap.Count == 0)
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                    {
                                        var body = TBody.Create(ghost, true, true);
                                        if (bodyMap == null)
                                            bodyMap = GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                        bodyMap.Set(body);
                                        action(body);
                                    }
                                }
                            }
                            else
                            {
                                var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                                while (enumerator.MoveNext())
                                {
                                    var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                    var body = bodyMap.Get(ghost.As<GhostHeader>()->Id, out var exist);
                                    if (exist)
                                    {
                                        if (body.Status != GhostStatus.MappedDeleted)
                                            action(body);
                                    }
                                    else
                                    {
                                        if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                        {
                                            body = TBody.Create(ghost, true, true);
                                            bodyMap.Set(body);
                                            action(body);
                                        }
                                    }
                                }
                            }
                            foreach (var id in bodyMap.InsertedIds)
                            {
                                var body = bodyMap.Get(id, out var exist);
                                if (exist && body.Status != GhostStatus.MappedDeleted)
                                {
                                    action(body);
                                }
                            }
                        }
                        else
                        {
                            var enumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                            while (enumerator.MoveNext())
                            {
                                var ghost = _txn.Repository.Store.ToGhost(enumerator.Current);
                                if (ghost.As<GhostHeader>()->Status != GhostStatus.Tombstone)
                                {
                                    var body = TBody.Create(ghost, true, true);
                                    if (bodyMap == null)
                                        bodyMap = GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    bodyMap.Set(body);
                                    action(body);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (bodyMap != null)
                        {
                            foreach (var id in bodyMap.InsertedIds)
                            {
                                var body = bodyMap.Get(id, out var exist);
                                if (exist && body.Status != GhostStatus.MappedDeleted)
                                {
                                    action(body);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
