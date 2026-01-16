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

using System.Collections;
using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
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

        public Enumerator<TBody> GetEnumerator<TBody>()
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            return new Enumerator<TBody>(this);
        }

        public struct Enumerator<TBody> : IEnumerator<TBody>
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            private readonly RepositoryTransactionBase _txn;
            private readonly ShardedTransactionBodyMap<TBody> _bodyMap;
            private readonly RepositoryTransactionBodyIndex _parent;
            private ShardedSegmentGhostMap<MemorySegmentStore>.ShardedDeduplicatedEnumerator _ghostEnumerator;
            private List<GhostId>.Enumerator _insertedIdsEnumerator;
            private TBody _current;
            private int _state; // 0: GhostMap, 1: InsertedIds, 2: Finished
            private bool _isReadOnly;
            private bool _hasBodyMap;
            private bool _isBodyMapEmpty;

            internal Enumerator(RepositoryTransactionBodyIndex parent)
            {
                _parent = parent;
                _txn = parent._txn;
                _isReadOnly = _txn.IsReadOnly;
                _bodyMap = parent.GetBodyMap<TBody>(TBody.GetTypeIdentifier());
                _hasBodyMap = _bodyMap != null;
                _isBodyMapEmpty = _hasBodyMap && _bodyMap.Count == 0;
                _current = null;
                _state = 0;
                _insertedIdsEnumerator = default;

                var map = _txn.Repository.GhostIndex.GetIndex(TBody.GetTypeIdentifier(), false);
                if (map != null)
                {
                    _ghostEnumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.OpeningTxnId);
                }
                else
                {
                    _ghostEnumerator = default;
                    _state = _isReadOnly ? 2 : 1; // Skip GhostMap if no index
                }

                if (!_isReadOnly && _hasBodyMap)
                {
                    // Prepare secondary enumerator for later, but do not start it yet.
                }
                else if (!_isReadOnly && !_hasBodyMap)
                {
                    // If no body map, we surely don't have inserted ids.
                    // But we might be in a state where we just iterate ghosts.
                }
            }

            public bool MoveNext()
            {
                if (_state == 2) return false;

                unsafe
                {
                    if (_state == 0)
                    {
                        // Iterating GhostMap
                        while (_ghostEnumerator.MoveNext())
                        {
                            var ghost = _txn.Repository.Store.ToGhost(_ghostEnumerator.Current);
                            var header = ghost.As<GhostHeader>();

                            if (header->Status == GhostStatus.Tombstone)
                                continue;

                            if (_isReadOnly)
                            {
                                if (_hasBodyMap)
                                {
                                    if (_isBodyMapEmpty)
                                    {
                                        // Case: ReadOnly + BodyMap Empty -> Just yield created bodies
                                        _current = TBody.Create(ghost, true, true);
                                        _bodyMap.Set(_current);
                                        return true;
                                    }
                                    else
                                    {
                                        // Case: ReadOnly + BodyMap Not Empty -> Check existence
                                        var body = _bodyMap.Get(header->Id, out var exist);
                                        if (exist)
                                        {
                                            if (body.Status != GhostStatus.MappedDeleted)
                                            {
                                                _current = body;
                                                return true;
                                            }
                                        }
                                        else
                                        {
                                            _current = TBody.Create(ghost, true, true);
                                            _bodyMap.Set(_current);
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    // Case: ReadOnly + No BodyMap -> Create, init map, set, yield
                                    _current = TBody.Create(ghost, true, true);
                                    // Lazy initialization of BodyMap inside iteration might be tricky with struct fields, 
                                    // but GetOrCreateBodyMap modifies the parent array, so next time _parent.GetBodyMap will return it.
                                    // However, our local _bodyMap field will be null.
                                    // We need to re-fetch or use parent.

                                    var bodyMap = _parent.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    // Update our local state if we just created it
                                    if (!_hasBodyMap)
                                    {
                                        // We can't easily update readonly struct field _bodyMap if we made it readonly.
                                        // But actually we can make it not readonly or just use a local var.
                                        // Let's assume we proceed.
                                        bodyMap.Set(_current);
                                    }
                                    else
                                    {
                                        _bodyMap.Set(_current);
                                    }

                                    return true;
                                }
                            }
                            else // ReadWrite
                            {
                                if (_hasBodyMap)
                                {
                                    if (_isBodyMapEmpty)
                                    {
                                        _current = TBody.Create(ghost, true, true);
                                        _bodyMap.Set(_current);
                                        return true;
                                    }
                                    else
                                    {
                                        var body = _bodyMap.Get(header->Id, out var exist);
                                        if (exist)
                                        {
                                            if (body.Status != GhostStatus.MappedDeleted)
                                            {
                                                _current = body;
                                                return true;
                                            }
                                        }
                                        else
                                        {
                                            _current = TBody.Create(ghost, true, true);
                                            _bodyMap.Set(_current);
                                            return true;
                                        }
                                    }
                                }
                                else
                                {
                                    // ReadWrite + No BodyMap
                                    _current = TBody.Create(ghost, true, true);
                                    var bodyMap = _parent.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    bodyMap.Set(_current);
                                    return true;
                                }
                            }
                        }

                        // Finished GhostMap
                        _state = 1;

                        if (_isReadOnly)
                        {
                            _state = 2; // Finished
                            return false;
                        }

                        // Prepare for InsertedIds
                        // We need the latest bodyMap reference in case it was created during iteration
                        var currentBodyMap = _parent.GetBodyMap<TBody>(TBody.GetTypeIdentifier());
                        if (currentBodyMap != null)
                        {
                            _insertedIdsEnumerator = currentBodyMap.InsertedIds.GetEnumerator();
                        }
                        else
                        {
                            _state = 2; // Nothing to iterate
                            return false;
                        }
                    }

                    if (_state == 1)
                    {
                        // Iterating InsertedIds (only for ReadWrite)
                        while (_insertedIdsEnumerator.MoveNext())
                        {
                            var id = _insertedIdsEnumerator.Current;
                            // Need to fetch from map again to get the object
                            var currentBodyMap = _parent.GetBodyMap<TBody>(TBody.GetTypeIdentifier()); // Should act. exist
                            var body = currentBodyMap.Get(id, out var exist);
                            if (exist && body.Status != GhostStatus.MappedDeleted)
                            {
                                _current = body;
                                return true;
                            }
                        }

                        _state = 2;
                        return false;
                    }

                    return false;
                }
            }

            public TBody Current => _current;

            object IEnumerator.Current => Current;

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
