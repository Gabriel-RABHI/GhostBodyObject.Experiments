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

using GhostBodyObject.Repository.Ghost.Constants;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    /// <summary>
    /// A 16-bit value that combines Kind (3 bits) and TypeIdentifier (13 bits).
    /// This struct wraps a ushort and provides zero-cost accessors for the sub-fields.
    /// Layout: [TypeIdentifier:13b | Kind:3b] (big-endian bit order within the ushort)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public readonly struct GhostTypeCombo : IEquatable<GhostTypeCombo>
    {
        private const int TypeShift = 3;
        private const ushort KindMask = 0x7;    // 3 bits
        private const ushort TypeMask = 0x1FFF; // 13 bits

        [FieldOffset(0)]
        private readonly ushort _value;

        /// <summary>
        /// Gets the raw ushort value.
        /// </summary>
        public ushort Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        /// <summary>
        /// Gets the Kind (3 bits, bits 0-2).
        /// </summary>
        public GhostIdKind Kind {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (GhostIdKind)(_value & KindMask);
        }

        /// <summary>
        /// Gets the TypeIdentifier (13 bits, bits 3-15).
        /// </summary>
        public ushort TypeIdentifier {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)((_value >> TypeShift) & TypeMask);
        }

        /// <summary>
        /// Creates a GhostTypeCombo from a raw ushort value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostTypeCombo(ushort value)
        {
            _value = value;
        }

        /// <summary>
        /// Creates a GhostTypeCombo from Kind and TypeIdentifier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostTypeCombo(GhostIdKind kind, ushort typeIdentifier)
        {
            ushort k = (ushort)((ushort)kind & KindMask);
            ushort t = (ushort)(typeIdentifier & TypeMask);
            _value = (ushort)((t << TypeShift) | k);
        }

        /// <summary>
        /// Implicit conversion from ushort to GhostTypeCombo.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GhostTypeCombo(ushort value) => new GhostTypeCombo(value);

        /// <summary>
        /// Implicit conversion from GhostTypeCombo to ushort.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ushort(GhostTypeCombo combo) => combo._value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostTypeCombo other) => _value == other._value;

        public override bool Equals(object? obj) => obj is GhostTypeCombo other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(GhostTypeCombo left, GhostTypeCombo right) => left._value == right._value;

        public static bool operator !=(GhostTypeCombo left, GhostTypeCombo right) => left._value != right._value;

        public override string ToString() => $"{Kind}-{TypeIdentifier}";
    }
}
