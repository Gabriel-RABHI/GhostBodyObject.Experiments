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

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct SegmentReference
    {
        // Static instances for comparison/assignment
        public static SegmentReference Empty => new SegmentReference { Value = 0 };
        public static SegmentReference Tombstone => new SegmentReference { Value = ulong.MaxValue };

        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        public uint SegmentId;

        [FieldOffset(4)]
        public uint Offset;

        [FieldOffset(0)]
        public ulong Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEmpty() => Value == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTombstone() => Value == ulong.MaxValue;

        // Helper to check if the slot has valid data (neither empty nor dead)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid() => Value != 0 && Value != ulong.MaxValue;
    }
}
