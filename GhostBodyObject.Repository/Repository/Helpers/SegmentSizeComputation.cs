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

#define SMALL

using GhostBodyObject.Repository.Repository.Constants;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public static class SegmentSizeComputation
    {
        public static int MB => 1024 * 1024;

        public static int GetNextSegmentSize(SegmentStoreMode _storeMode, int segmentCount, int recordSize = 0)
        {
            var r = GetNextSegmentSizeBase(_storeMode, segmentCount, recordSize);

            if (recordSize > 0 && r < recordSize * 2)
            {
                if (recordSize >= 512 * MB)
                    throw new InvalidOperationException($"Unsupported Segment size (more the 1 GB) imposed by next record size ({recordSize}).");
                while (r < recordSize * 2)
                {
                    r *= 2;
                }
            }
            return r;
        }

#if SMALL
        private static int GetNextSegmentSizeBase(SegmentStoreMode _storeMode, int segmentCount, int recordSize = 0)
        {
            var r = 0;
            switch (_storeMode)
            {
                case SegmentStoreMode.InMemoryVolatileRepository:
                    if (segmentCount < 16)
                        return 1 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 64)
                        return 2 * MB; // 32 * 24 = 768 MB
                    return 4 * MB;
                    break;
                case SegmentStoreMode.InVirtualMemoryVolatileRepository:
                    if (segmentCount < 16)
                        return 1 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 64)
                        return 2 * MB; // 32 * 24 = 768 MB
                    return 4 * MB;
                    break;
                case SegmentStoreMode.InMemoryVolatileLog:
                    return 2 * MB;
                    break;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    if (segmentCount < 8)
                        return 1 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 16)
                        return 2 * MB; // 16 * 8 = 512 MB
                    if (segmentCount < 32)
                        return 4 * MB; // 256 * 16 = 8 GB (8.6 GB total)
                    return 8 * MB;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }
#else
        private static int GetNextSegmentSizeBase(SegmentStoreMode _storeMode, int segmentCount, int recordSize = 0)
        {
            var r = 0;
            switch (_storeMode)
            {
                case SegmentStoreMode.InMemoryVolatileRepository:
                    if (segmentCount < 16)
                        return 8 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 64)
                        return 32 * MB; // 32 * 24 = 768 MB
                    return 128 * MB;
                    break;
                case SegmentStoreMode.InVirtualMemoryVolatileRepository:
                    if (segmentCount < 16)
                        return 16 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 64)
                        return 64 * MB; // 32 * 24 = 768 MB
                    return 256 * MB;
                    break;
                case SegmentStoreMode.InMemoryVolatileLog:
                    return 32 * MB;
                    break;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    if (segmentCount < 8)
                        return 16 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 16)
                        return 64 * MB; // 16 * 8 = 512 MB
                    if (segmentCount < 32)
                        return 256 * MB; // 256 * 16 = 8 GB (8.6 GB total)
                    return 1024 * MB;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }
#endif
    }
}
