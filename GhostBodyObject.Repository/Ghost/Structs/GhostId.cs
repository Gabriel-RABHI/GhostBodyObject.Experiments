using GhostBodyObject.Common.Utilities;
using GhostBodyObject.Repository.Ghost.Constants;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    /// <summary>
    /// A 16-byte unmanaged Ghost Identifier.
    /// Layout: [Kind:3b | Type:13b | When:48b] [Random:64b]
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct GhostId : IEquatable<GhostId>, IComparable<GhostId>
    {
        public const ushort MAX_TYPE_ID = 0x1FFF; // 13 bits
        public const ushort MAX_KIND = 0x007; // 3 bits
        public const ushort MAX_TYPE_COMBO = ushort.MaxValue; // 16 bits

        // ---------------------------------------------------------
        // Field Layout
        // ---------------------------------------------------------
        // Field 1: Header (8 bytes)
        // Bits 61-63 (3 bits)  : Kind
        // Bits 48-60 (13 bits) : Type Identifier
        // Bits 00-47 (48 bits) : Timestamp (Microseconds)

        [FieldOffset(0)]
        private readonly ulong _header;

        // Field 2: Random (8 bytes)
        // Bits 00-63 (64 bits) : High-entropy random
        [FieldOffset(8)]
        private readonly ulong _random;

        /// <summary>
        /// Bits 00-31 of the Random part. Used for Map Slot Computation.
        /// </summary>
        [FieldOffset(8)]
        public readonly int SlotComputation;

        /// <summary>
        /// Bits 32-47 of the Random part. Used for the Fast Filter Tag (Short[]).
        /// </summary>
        [FieldOffset(12)]
        public readonly short RandomPartTag;

        /// <summary>
        /// Bits 48-63 of the Random part. Used for Shard Selection.
        /// </summary>
        [FieldOffset(14)]
        public readonly short ShardComputation;

        [FieldOffset(0)]
        private readonly GhostTypeCombo _typeCombo;

        // ---------------------------------------------------------
        // Constants & Masks
        // ---------------------------------------------------------
        private const int KindShift = 61;
        private const int TypeShift = 48;
        private const ulong TimestampMask = 0x0000_FFFF_FFFF_FFFF;
        private const ulong TypeMask = 0x1FFF; // 13 bits
        private const ulong KindMask = 0x7;    // 3 bits

        // Epoch: 2024-01-01 (Adjust as needed to reset the 9-year loop)
        private static readonly long EpochTicks = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        public ulong RandomPart
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _random;
        }



        // ---------------------------------------------------------
        // Construction
        // ---------------------------------------------------------
        /// <summary>
        /// Generates a new unique ObjectId.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GhostId NewId(GhostIdKind kind, ushort typeId)
        {
            // 1. Get current time in microseconds
            // (DateTime.UtcNow.Ticks is 100ns resolution. Divide by 10 for microseconds)
            ulong nowUs = (ulong)((DateTime.UtcNow.Ticks - EpochTicks) / 10);

            // 2. Generate fast random
            ulong rng = XorShift64.Next();

            return new GhostId(kind, typeId, nowUs, rng);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostId(GhostIdKind kind, ushort typeId, ulong timestamp, ulong random)
        {
            // Clamp inputs to ensure they don't overwrite neighbor bits
            ulong k = (ulong)kind & KindMask;
            ulong t = (ulong)typeId & TypeMask;
            ulong w = timestamp & TimestampMask;

            // Pack Header
            _header = (k << KindShift) | (t << TypeShift) | w;
            _random = random;
        }

        // ---------------------------------------------------------
        // Accessors (Zero-Cost unpacking)
        // ---------------------------------------------------------
        public GhostIdKind Kind
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (GhostIdKind)(_header >> KindShift);
        }

        public ushort TypeIdentifier
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)((_header >> TypeShift) & TypeMask);
        }

        public DateTime CreatedAt
        {
            get
            {
                long ticks = (long)(_header & TimestampMask) * 10;
                return new DateTime(EpochTicks + ticks, DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Gets the TypeCombo which combines Kind and TypeIdentifier in a single 16-bit value.
        /// </summary>
        public GhostTypeCombo TypeCombo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _typeCombo;
        }

        // ---------------------------------------------------------
        // Equality & Comparison (Optimized)
        // ---------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostId other) => _header == other._header && _random == other._random;

        public override bool Equals(object? obj) => obj is GhostId other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_header, _random);

        public static bool operator ==(GhostId left, GhostId right) => left.Equals(right);

        public static bool operator !=(GhostId left, GhostId right) => !left.Equals(right);

        public int CompareTo(GhostId other)
        {
            int headerCmp = _header.CompareTo(other._header);
            if (headerCmp != 0)
                return headerCmp;
            return _random.CompareTo(other._random);
        }

        public override string ToString() => $"{Kind}-{TypeIdentifier}-{_header & TimestampMask:X}-{_random:X}";
    }
}
