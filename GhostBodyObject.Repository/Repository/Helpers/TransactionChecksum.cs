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

using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public sealed class TransactionChecksum : IDisposable
    {
        private readonly XxHash3 _hasher;

        public TransactionChecksum()
        {
            // System.IO.Hashing.XxHash3 is optimized for speed and SIMD.
            _hasher = new XxHash3();
        }

        /// <summary>
        /// Writes a raw memory block to the running hash.
        /// Fast: Zero allocations, uses Spans.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(byte* data, int size)
        {
            // Create a span around the pointer. This is a stack-only struct operation (fast).
            var span = new ReadOnlySpan<byte>(data, size);
            _hasher.Append(span);
        }

        /// <summary>
        /// Writes any unmanaged struct (int, float, custom structs) to the running hash.
        /// Fast: No boxing, treats the struct memory directly as bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T value) where T : unmanaged
        {
            // Treat the reference of 'value' as a Span of bytes.
            // This avoids copying the struct to a byte array.
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateReadOnlySpan(ref value, 1)
            );

            _hasher.Append(bytes);
        }

        /// <summary>
        /// Writes a standard byte span (useful for managed arrays/buffers).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> data)
        {
            _hasher.Append(data);
        }

        /// <summary>
        /// Returns the current 64-bit checksum.
        /// </summary>
        public ulong GetHash()
        {
            // XxHash3 produces a 64-bit hash. 
            // We retrieve it into a ulong (little-endian by default in this API).
            Span<byte> destination = stackalloc byte[sizeof(ulong)];
            _hasher.GetCurrentHash(destination);
            return MemoryMarshal.Read<ulong>(destination);
        }

        /// <summary>
        /// Resets the hasher for a new transaction validation.
        /// </summary>
        public void Reset()
        {
            _hasher.Reset();
        }

        public void Dispose()
        {
            // XxHash3 usually doesn't hold unmanaged resources, but good practice if implementation changes.
            // Currently, System.IO.Hashing implementation is purely managed/stack based logic 
            // but keeping Dispose ensures forward compatibility.
        }
    }
}
