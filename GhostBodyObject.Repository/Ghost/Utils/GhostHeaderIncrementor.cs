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

using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Experiments.BabyBody
{
    /// <summary>
    /// Provides functionality for incrementally calculating memory offsets for ghost header structures, supporting
    /// type-based size advancement and alignment padding.
    /// </summary>
    /// <remarks>This class is typically used when constructing or serializing binary data structures that
    /// require precise control over field offsets and alignment. Offsets are advanced based on the size of types or by
    /// applying specific padding to meet alignment requirements. The initial offset is set to the size of the ghost
    /// header structure.</remarks>
    public class GhostHeaderIncrementor
    {
        private int _offset = GhostHeader.SIZE;

        public int Push<T>()
        {
            int currentOffset = _offset;
            _offset += Unsafe.SizeOf<T>();
            return currentOffset;
        }

        public int Padd(int padding)
        {
            if(padding != 2 && padding != 4 && padding != 8 && padding != 16)
                throw new System.ArgumentException("Padding must be 2, 4, 8, or 16 bytes.");
            _redo:
            if ((_offset % padding) != 0)
            {
                _offset++;
                goto _redo;
            }
            return _offset;
        }

        public int Offset => _offset;
    }
}
