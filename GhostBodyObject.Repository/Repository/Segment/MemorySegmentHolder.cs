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

#define SAFE

using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public sealed class MemorySegmentHolder : IDisposable
    {
        private long _usedMemoryVolume = 0;
        private int _referenceCount = 0;

        public MemorySegment Segment { get; set; }

        public int Index { get; set; }

        public int ReferenceCount => _referenceCount;

        public long UsedMemoryVolume => _usedMemoryVolume;

        public bool Forgotten => Segment == null;

        public bool IsEmpty => Segment == null || Segment.IsEmpty;

        public MemorySegmentHolder(MemorySegment segment, int index)
        {
            Segment = segment;
            Index = index;
        }

        public void Dispose()
        {
            Segment = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementUsage(long size)
        {
#if SAFE
            Interlocked.Increment(ref _referenceCount);
            Interlocked.Add(ref _usedMemoryVolume, size);
#else
            _referenceCount++;
            _usedMemoryVolume += size;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool DecrementUsage(long size)
        {
#if SAFE
            Interlocked.Add(ref _usedMemoryVolume, -size);
            return Interlocked.Decrement(ref _referenceCount) == 0;
#else
            _referenceCount--;
            _usedMemoryVolume -= size;
            return _referenceCount <= 0;
#endif
        }
    }
}
