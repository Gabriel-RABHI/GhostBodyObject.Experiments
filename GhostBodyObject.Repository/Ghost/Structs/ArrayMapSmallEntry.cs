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

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public unsafe struct ArrayMapSmallEntry
    {
        // Constants
        private const ushort ValueSizeMask = 0x1F;
        private const ushort ArrayLengthMask = 0x7FF;

        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        private ushort _lowerHalf;  // ValueSize (5) + ArrayLength (11)
        [FieldOffset(2)]
        public ushort ArrayOffset;  // Direct UInt16 access

        // -----------------------------------------------------------------
        // PROPERTIES
        // -----------------------------------------------------------------

        public uint ValueSize {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)(_lowerHalf & ValueSizeMask);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lowerHalf = (ushort)((_lowerHalf & ~ValueSizeMask) | (value & ValueSizeMask));
        }

        public uint ArrayLength {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (uint)((_lowerHalf >> 5) & ArrayLengthMask);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _lowerHalf = (ushort)((_lowerHalf & ~(ArrayLengthMask << 5)) | ((value & ArrayLengthMask) << 5));
        }

        // -----------------------------------------------------------------
        // COMPUTED PROPERTIES (New)
        // -----------------------------------------------------------------

        public int PhysicalSize {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
        }

        public int ArrayEndOffset {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                // ValueSize * ArrayLength
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return ArrayOffset + size;
            }
        }

        public int ArrayEndIntPaddedOffset {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return (ArrayOffset + size + 3) & ~3;
            }
        }

        public int ArrayEndLongPaddedOffset {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                int size = (_lowerHalf & ValueSizeMask) * ((_lowerHalf >> 5) & ArrayLengthMask);
                return (ArrayOffset + size + 7) & ~7;
            }
        }
    }
}
