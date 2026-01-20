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

        public RepositoryTransactionBase Transaction => _txn;

        public TBody GetBody<TBody>(GhostId id)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            var bodyMap = GetBodyMap<TBody>(TBody.GetTypeIdentifier());
            var body = bodyMap.Get(id, out var exists);
            if (exists)
                return body;
            return null;
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

        public Enumerator<TBody> GetEnumerator<TBody>(bool useCursor = false)
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            return new Enumerator<TBody>(this, useCursor);
        }

        public struct Enumerator<TBody> : IEnumerator<TBody>
            where TBody : BodyBase, IHasTypeIdentifier, IBodyFactory<TBody>
        {
            private readonly RepositoryTransactionBase _txn;
            private readonly ShardedTransactionBodyMap<TBody> _bodyMap;
            private readonly RepositoryTransactionBodyIndex _parent;
            private ShardedSegmentGhostMap<MemorySegmentStoreHolders>.ShardedDeduplicatedEnumerator _ghostEnumerator;
            private List<GhostId>.Enumerator _insertedIdsEnumerator;
            private TBody _current;
            private int _state; // 0: GhostMap, 1: InsertedIds, 2: Finished
            private bool _isReadOnly;
            private bool _hasBodyMap;
            private bool _isBodyMapEmpty;
            private bool _useCursor;
            private TBody _cursorBody;
            private GhostStatus _cursorStatus;

            internal Enumerator(RepositoryTransactionBodyIndex parent, bool useCursor)
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
                _useCursor = useCursor;
                _cursorBody = null;
                _cursorStatus = default;

                var map = _txn.Repository.GhostIndex.GetIndex(TBody.GetTypeIdentifier(), false);
                if (map != null)
                {
                    _ghostEnumerator = map.GhostMap.GetDeduplicatedEnumerator(_txn.Holders, _txn.OpeningTxnId);
                }
                else
                {
                    _ghostEnumerator = default;
                    _state = _isReadOnly ? 2 : 1; // Skip GhostMap if no index
                    if (_state == 1)
                    {
                        if (_hasBodyMap)
                            _insertedIdsEnumerator = _bodyMap.InsertedIds.GetEnumerator();
                        else
                            _state = 2;
                    }
                }

                if (!_isReadOnly && _hasBodyMap)
                {
                    // Prepare secondary enumerator for later, but do not start it yet.
                }
            }

            public bool MoveNext()
            {
                if (_state == 2)
                    return false;
                unsafe
                {
                    if (_state == 0)
                    {
                        // Iterating GhostMap
                        while (_ghostEnumerator.MoveNext())
                        {
                            var ghost = _txn.Holders.ToGhost(_ghostEnumerator.Current);
                            var header = ghost.As<GhostHeader>();

                            if (header->Status == GhostStatus.Tombstone)
                                continue;

                            // -------- Check if the body is already in the map (modified/inserted/read) --------
                            // If it is in the map, we MUST yield the map instance to preserve object identity.
                            if (_hasBodyMap)
                            {
                                if (!_isBodyMapEmpty)
                                {
                                    var body = _bodyMap.Get(header->Id, out var exist);
                                    if (exist)
                                    {
                                        if (body.Status != GhostStatus.MappedDeleted)
                                        {
                                            _current = body;
                                            return true;
                                        }
                                        continue; // Deleted in map, skip
                                    }
                                }
                            }
                            // Note: If no body map or not found, proceed to yield ghost.

                            // -------- Cursor Mode Logic --------
                            if (_useCursor)
                            {
                                // If we have a cursor body, check if it was modified (detached)
                                if (_cursorBody != null)
                                {
                                    if (_cursorBody.Status != _cursorStatus)
                                    {
                                        // Status changed: The user modified the body. It is now "detached".
                                        // We drop our reference to it (creating a new cursor for this iteration)
                                        // so that we don't overwrite the user's modified object.
                                        _cursorBody = null;
                                    }
                                }

                                if (_cursorBody == null)
                                {
                                    _cursorBody = TBody.Create(ghost, true, true);
                                }
                                else
                                {
                                    _cursorBody.SwapGhost(ghost);
                                    // Status is implicitly updated by swapping data.
                                }

                                // Memorize status for next iteration check
                                _cursorStatus = _cursorBody.Status;
                                _current = _cursorBody;
                                // Important: We do NOT add cursor body to _bodyMap.
                                return true;
                            }

                            // -------- Standard Mode (Allocation per item) --------
                            // If we are here, we are not using cursor, and it wasn't in the map.

                            _current = TBody.Create(ghost, true, true);

                            if (!_isReadOnly)
                            {
                                // In ReadWrite, we must add to map?
                                // Original logic added to map.
                                if (_bodyMap == null)
                                {
                                    var bodyMap = _parent.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    bodyMap.Set(_current);
                                }
                                else
                                {
                                    _bodyMap.Set(_current);
                                }
                            }
                            else
                            {
                                // In ReadOnly
                                if (_bodyMap == null)
                                {
                                    var bodyMap = _parent.GetOrCreateBodyMap<TBody>(TBody.GetTypeIdentifier());
                                    bodyMap.Set(_current);
                                }
                                else
                                {
                                    _bodyMap.Set(_current);
                                }
                            }
                            return true;
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
                            var currentBodyMap = _parent.GetBodyMap<TBody>(TBody.GetTypeIdentifier());
                            var body = currentBodyMap.Get(id, out var exist);
                            if (exist && (body.Status != GhostStatus.MappedDeleted))
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
