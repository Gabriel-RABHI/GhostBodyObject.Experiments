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
using GhostBodyObject.Repository.Repository.Index;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Transaction.Index;
using System.Collections;

namespace GhostBodyObject.Repository.Repository.Transaction.Collections
{
    public struct BodyCollection<TBody> : IEnumerable<TBody>
        where TBody : BodyBase
    {
        private ShardedTransactionBodyMap<TBody> _map;
        private RepositoryGhostIndex<MemorySegmentStore> _store;

        public int Count => _map.Count;

        public BodyCollection(ShardedTransactionBodyMap<TBody> map, RepositoryGhostIndex<MemorySegmentStore> store)
        {
            _map = map;
            _store = store;
        }

        public IEnumerator<TBody> GetEnumerator() => _map.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _map.GetEnumerator();

        public IEnumerable<TBody> Filter(Func<TBody, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TBody> Scan(Action<TBody> action)
        {
            throw new NotImplementedException();
        }

        public void ForEach(Action<TBody> action)
        {
            foreach (var body in _map)
            {
                action(body);
            }
        }
    }
}
