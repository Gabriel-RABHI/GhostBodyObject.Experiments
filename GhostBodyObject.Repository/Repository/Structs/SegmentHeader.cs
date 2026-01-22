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

using GhostBodyObject.Repository.Repository.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct SegmentHeader
    {
        [FieldOffset(0)]
        public SegmentStructureType T;

        [FieldOffset(1)]
        public SegmentStoreMode Mode;

        [FieldOffset(2)]
        public ushort Empty;

        [FieldOffset(4)]
        public int SegmentId;

        [FieldOffset(8)]
        public int Capacity;

        [FieldOffset(12)]
        public int HeadPosition;

        public static SegmentHeader Create(SegmentStoreMode mode, int segmentId, int capacity)
        {
            return new SegmentHeader {
                T = SegmentStructureType.SegmentHeader,
                Mode = mode,
                Empty = 0,
                SegmentId = segmentId,
                Capacity = capacity,
                HeadPosition = 0
            };
        }
    }
}
