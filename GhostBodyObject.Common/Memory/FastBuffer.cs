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

public static class FastBuffer
{
    // SkipLocalsInit: Prevents the JIT from zeroing local variables/stack, saving tiny cycles.
    [ModuleInitializer]
    internal static void Init()
    {
        // This method ensures the module is initialized; 
        // SkipLocalsInit applies at method level usually.
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static unsafe void Set<T>(byte* ptr, int offset, T value) where T : struct
        => *(T*)(ptr + offset) = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static unsafe T Get<T>(byte* ptr, int offset) where T : struct
        => *(T*)(ptr + offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void Set<T>(PinnedMemory<byte> buffer, int offset, T value) where T : struct
    {
        Set(buffer.Span, offset, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static T Get<T>(PinnedMemory<byte> buffer, int offset) where T : struct
    {
        return Get<T>(buffer.Span, offset);
    }

    // -------------------------------------------------------------------------
    // WRITE OPERATIONS
    // -------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void Set<T>(byte[] buffer, int offset, T value) where T : struct
    {
        // 1. Get raw reference to the first byte of the array (No Bounds Check)
        ref byte start = ref MemoryMarshal.GetArrayDataReference(buffer);

        // 2. Add offset (Pointer Arithmetic)
        ref byte target = ref Unsafe.Add(ref start, offset);

        // 3. Write unaligned (Fastest safe generic write)
        Unsafe.WriteUnaligned(ref target, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void Set<T>(Span<byte> buffer, int offset, T value) where T : struct
    {
        ref byte start = ref MemoryMarshal.GetReference(buffer);
        ref byte target = ref Unsafe.Add(ref start, offset);
        Unsafe.WriteUnaligned(ref target, value);
    }

    // -------------------------------------------------------------------------
    // READ OPERATIONS
    // -------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static T Get<T>(byte[] buffer, int offset) where T : struct
    {
        ref byte start = ref MemoryMarshal.GetArrayDataReference(buffer);
        ref byte source = ref Unsafe.Add(ref start, offset);
        return Unsafe.ReadUnaligned<T>(ref source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static T Get<T>(ReadOnlySpan<byte> buffer, int offset) where T : struct
    {
        ref byte start = ref MemoryMarshal.GetReference(buffer);
        ref byte source = ref Unsafe.Add(ref start, offset);
        return Unsafe.ReadUnaligned<T>(ref source);
    }

    // -------------------------------------------------------------------------
    // BULK COPY: Struct[] -> Byte[]
    // -------------------------------------------------------------------------

    /// <summary>
    /// Copies an entire array of structs into a byte buffer at the specified offset.
    /// Fast as memcpy.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void WriteArray<T>(byte[] destination, int destOffset, T[] source) where T : struct
    {
        // 1. Get reference to destination (byte buffer)
        ref byte destRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(destination), destOffset);

        // 2. Get reference to source (struct array) as bytes
        // We reinterpret the struct array as a stream of bytes immediately
        ref byte srcRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(source));

        // 3. Calculate total bytes to copy
        // SizeOf<T>() is a JIT constant, very fast.
        uint byteCount = (uint)(source.Length * Unsafe.SizeOf<T>());

        // 4. Bulk Copy
        Unsafe.CopyBlockUnaligned(ref destRef, ref srcRef, byteCount);
    }

    // -------------------------------------------------------------------------
    // BULK COPY: Byte[] -> Struct[]
    // -------------------------------------------------------------------------

    /// <summary>
    /// Reads a chunk of bytes directly into an array of structs.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void ReadArray<T>(byte[] source, int srcOffset, T[] destination) where T : struct
    {
        // 1. Get reference to source (byte buffer)
        ref byte srcRef = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(source), srcOffset);

        // 2. Get reference to destination (struct array)
        ref byte destRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(destination));

        // 3. Calculate size
        uint byteCount = (uint)(destination.Length * Unsafe.SizeOf<T>());

        // 4. Bulk Copy
        Unsafe.CopyBlockUnaligned(ref destRef, ref srcRef, byteCount);
    }

    // -------------------------------------------------------------------------
    // BULK COPY: Span-Based (Works for Memory<byte> too)
    // -------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SkipLocalsInit]
    public static void WriteSpan<T>(Span<byte> destination, int destOffset, ReadOnlySpan<T> source) where T : struct
    {
        // 1. Destination Ref
        ref byte destRef = ref Unsafe.Add(ref MemoryMarshal.GetReference(destination), destOffset);

        // 2. Source Ref (Reinterpreted)
        ref byte srcRef = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(source));

        // 3. Size
        uint byteCount = (uint)(source.Length * Unsafe.SizeOf<T>());

        // 4. Copy
        Unsafe.CopyBlockUnaligned(ref destRef, ref srcRef, byteCount);
    }
}