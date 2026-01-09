using GhostBodyObject.Repository.Ghost.Constants;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    /// <summary>
    /// A 16-bit value that combines Kind (3 bits) and TypeIdentifier (13 bits).
    /// This struct wraps a ushort and provides zero-cost accessors for the sub-fields.
    /// Layout: [Kind:3b | TypeIdentifier:13b] (big-endian bit order within the ushort)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public readonly struct GhostTypeCombo : IEquatable<GhostTypeCombo>
    {
        private const int KindShift = 13;
        private const ushort TypeMask = 0x1FFF; // 13 bits
        private const ushort KindMask = 0x7;    // 3 bits

        [FieldOffset(0)]
        private readonly ushort _value;

        /// <summary>
        /// Gets the raw ushort value.
        /// </summary>
        public ushort Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
        }

        /// <summary>
        /// Gets the Kind (3 bits, bits 13-15).
        /// </summary>
        public GhostIdKind Kind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (GhostIdKind)((_value >> KindShift) & KindMask);
        }

        /// <summary>
        /// Gets the TypeIdentifier (13 bits, bits 0-12).
        /// </summary>
        public ushort TypeIdentifier
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)(_value & TypeMask);
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
            _value = (ushort)((k << KindShift) | t);
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
