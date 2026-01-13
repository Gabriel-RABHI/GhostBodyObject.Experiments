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
using System.Drawing;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public static class SegmentStoreModeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SegmentImplementationType ImplementationMode(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.InMemoryLog:
                    return SegmentImplementationType.LOHPinnedMemory;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    return SegmentImplementationType.ProtectedMemoryMappedFile;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPersistent(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.InMemoryLog:
                    return false;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    return true;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompactable(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.PersistantRepository:
                    return true;
                case SegmentStoreMode.InMemoryLog:
                case SegmentStoreMode.PersistantLog:
                    return false;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }
    }
}
