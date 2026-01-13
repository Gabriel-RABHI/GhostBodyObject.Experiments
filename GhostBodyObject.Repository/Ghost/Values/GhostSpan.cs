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

using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Values
{
    /// <summary>
    /// A ref struct that wraps array data of unmanaged type T stored in pinned memory, providing span-like semantics
    /// with efficient zero-copy reads and minimal-copy writes. Designed for high-performance array manipulation
    /// within Ghost Body entities.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    public ref struct GhostSpan<T> where T : unmanaged
    {
        private readonly BodyBase _body;
        private readonly PinnedMemory<byte> _data;
        private readonly int _arrayIndex;

        // When created from an array, we store the array directly and use its span
        // This avoids storing raw pointers to managed memory which could be moved by GC
        private readonly T[]? _sourceArray;

        // -------------------------------------------------------------------------
        // CONSTRUCTORS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of GhostSpan with the specified body, array index, and pinned memory data.
        /// </summary>
        public GhostSpan(BodyBase body, int arrayIndex, PinnedMemory<byte> data)
        {
            _body = body;
            _arrayIndex = arrayIndex;
            _data = data;
            _sourceArray = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostSpan with only pinned memory data (read-only access).
        /// </summary>
        public GhostSpan(PinnedMemory<byte> data)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = data;
            _sourceArray = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostSpan from an array (read-only, used for implicit conversion).
        /// This is the safe way to wrap an array without storing raw pointers.
        /// </summary>
        private GhostSpan(T[] source)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = default;
            _sourceArray = source;
        }

        // -------------------------------------------------------------------------
        // PROPERTIES
        // -------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the array in elements.
        /// </summary>
        public int Length => _sourceArray != null ? _sourceArray.Length : _data.Length / Unsafe.SizeOf<T>();

        /// <summary>
        /// Gets the length of the underlying data in bytes.
        /// </summary>
        public int ByteLength => _sourceArray != null ? _sourceArray.Length * Unsafe.SizeOf<T>() : _data.Length;

        /// <summary>
        /// Gets a value indicating whether this array is empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Length)
                    ThrowIndexOutOfRange();
                return AsSpan()[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= (uint)Length)
                    ThrowIndexOutOfRange();
                AsWritableSpan()[index] = value;
            }
        }

        // -------------------------------------------------------------------------
        // SPAN ACCESS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the data as a ReadOnlySpan of elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsSpan()
        {
            if (_sourceArray != null)
                return _sourceArray.AsSpan();

            return MemoryMarshal.Cast<byte, T>(_data.Span);
        }

        /// <summary>
        /// Returns the data as a Span of elements (for modification).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the source is an array (read-only in this context).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsWritableSpan()
        {
            if (_sourceArray != null)
                ThrowReadOnly();

            return MemoryMarshal.Cast<byte, T>(_data.Span);
        }

        /// <summary>
        /// Returns the underlying bytes as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsBytes()
        {
            if (_sourceArray != null)
                return MemoryMarshal.AsBytes(_sourceArray.AsSpan());

            return _data.Span;
        }

        /// <summary>
        /// Returns a portion of the data as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsSpan(int start) => AsSpan().Slice(start);

        /// <summary>
        /// Returns a portion of the data as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsSpan(int start, int length) => AsSpan().Slice(start, length);

        // -------------------------------------------------------------------------
        // ARRAY MODIFICATION - COMPLETE REPLACEMENT
        // -------------------------------------------------------------------------

        /// <summary>
        /// Sets the array value, updating the underlying entity body.
        /// </summary>
        public unsafe void SetArray(T[] value)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, MemoryMarshal.AsBytes(value.AsSpan()), _arrayIndex);
        }

        /// <summary>
        /// Sets the array value from another GhostSpan.
        /// </summary>
        public unsafe void SetArray(GhostSpan<T> value)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, value.AsBytes(), _arrayIndex);
        }

        /// <summary>
        /// Sets the array value from a ReadOnlySpan of elements.
        /// </summary>
        public unsafe void SetArray(ReadOnlySpan<T> value)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, MemoryMarshal.AsBytes(value), _arrayIndex);
        }

        /// <summary>
        /// Clears the array (sets length to 0).
        /// </summary>
        public unsafe void Clear()
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, ReadOnlySpan<byte>.Empty, _arrayIndex);
        }

        // -------------------------------------------------------------------------
        // HIGH-PERFORMANCE ARRAY MODIFICATION - IN-PLACE OPERATIONS
        // These methods modify the array in place with minimal memory operations
        // -------------------------------------------------------------------------

        /// <summary>
        /// Appends a single element to the end of the array.
        /// </summary>
        public unsafe void Append(T value)
        {
            if (_body == null)
                ThrowReadOnly();

            var valueBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value));
            BodyBase.AppendToArray(_body, valueBytes, _arrayIndex);
        }

        /// <summary>
        /// Appends multiple elements to the end of the array.
        /// </summary>
        public unsafe void AppendRange(ReadOnlySpan<T> values)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.AppendToArray(_body, MemoryMarshal.AsBytes(values), _arrayIndex);
        }

        /// <summary>
        /// Appends elements from another GhostSpan to the end of the array.
        /// </summary>
        public void AppendRange(GhostSpan<T> values)
        {
            AppendRange(values.AsSpan());
        }

        /// <summary>
        /// Prepends a single element to the beginning of the array.
        /// </summary>
        public unsafe void Prepend(T value)
        {
            if (_body == null)
                ThrowReadOnly();

            var valueBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value));
            BodyBase.PrependToArray(_body, valueBytes, _arrayIndex);
        }

        /// <summary>
        /// Prepends multiple elements to the beginning of the array.
        /// </summary>
        public unsafe void PrependRange(ReadOnlySpan<T> values)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.PrependToArray(_body, MemoryMarshal.AsBytes(values), _arrayIndex);
        }

        /// <summary>
        /// Prepends elements from another GhostSpan to the beginning of the array.
        /// </summary>
        public void PrependRange(GhostSpan<T> values)
        {
            PrependRange(values.AsSpan());
        }

        /// <summary>
        /// Inserts a single element at the specified index.
        /// </summary>
        public unsafe void InsertAt(int index, T value)
        {
            if (_body == null)
                ThrowReadOnly();
            if ((uint)index > (uint)Length)
                ThrowIndexOutOfRange();

            var valueBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value));
            BodyBase.InsertIntoArray(_body, valueBytes, _arrayIndex, index * Unsafe.SizeOf<T>());
        }

        /// <summary>
        /// Inserts multiple elements at the specified index.
        /// </summary>
        public unsafe void InsertRangeAt(int index, ReadOnlySpan<T> values)
        {
            if (_body == null)
                ThrowReadOnly();
            if ((uint)index > (uint)Length)
                ThrowIndexOutOfRange();

            BodyBase.InsertIntoArray(_body, MemoryMarshal.AsBytes(values), _arrayIndex, index * Unsafe.SizeOf<T>());
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        public unsafe void RemoveAt(int index)
        {
            if (_body == null)
                ThrowReadOnly();
            if ((uint)index >= (uint)Length)
                ThrowIndexOutOfRange();

            int elementSize = Unsafe.SizeOf<T>();
            BodyBase.RemoveFromArray(_body, _arrayIndex, index * elementSize, elementSize);
        }

        /// <summary>
        /// Removes a range of elements starting at the specified index.
        /// </summary>
        public unsafe void RemoveRange(int startIndex, int count)
        {
            if (_body == null)
                ThrowReadOnly();
            if (startIndex < 0 || count < 0 || (uint)(startIndex + count) > (uint)Length)
                ThrowIndexOutOfRange();
            if (count == 0)
                return;

            int elementSize = Unsafe.SizeOf<T>();
            BodyBase.RemoveFromArray(_body, _arrayIndex, startIndex * elementSize, count * elementSize);
        }

        /// <summary>
        /// Removes the first occurrence of the specified value.
        /// </summary>
        /// <returns>True if the element was found and removed; otherwise, false.</returns>
        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Removes all occurrences of the specified value.
        /// </summary>
        /// <returns>The number of elements removed.</returns>
        public int RemoveAll(T value)
        {
            int count = 0;
            int currentLength = Length;
            int index;
            while (currentLength > 0 && (index = IndexOfInRange(value, currentLength)) >= 0)
            {
                RemoveAt(index);
                currentLength--;
                count++;
            }
            return count;
        }

        // Helper method for searching within a specified range
        private int IndexOfInRange(T value, int length)
        {
            var span = AsSpan();
            int searchLength = Math.Min(length, span.Length);
            for (int i = 0; i < searchLength; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Replaces a range of elements with new values.
        /// </summary>
        public unsafe void ReplaceRange(int startIndex, int count, ReadOnlySpan<T> replacement)
        {
            if (_body == null)
                ThrowReadOnly();
            if (startIndex < 0 || count < 0 || (uint)(startIndex + count) > (uint)Length)
                ThrowIndexOutOfRange();

            int elementSize = Unsafe.SizeOf<T>();
            BodyBase.ReplaceInArray(_body, MemoryMarshal.AsBytes(replacement), _arrayIndex, startIndex * elementSize, count * elementSize);
        }

        /// <summary>
        /// Replaces a range of elements with values from another GhostSpan.
        /// </summary>
        public void ReplaceRange(int startIndex, int count, GhostSpan<T> replacement)
        {
            ReplaceRange(startIndex, count, replacement.AsSpan());
        }

        /// <summary>
        /// Removes the last element and returns it.
        /// </summary>
        public T Pop()
        {
            int len = Length;
            if (len == 0)
                ThrowEmptyArray();
            T value = AsSpan()[len - 1];
            RemoveAt(len - 1);
            return value;
        }

        /// <summary>
        /// Removes the first element and returns it.
        /// </summary>
        public T Shift()
        {
            if (Length == 0)
                ThrowEmptyArray();
            T value = AsSpan()[0];
            RemoveAt(0);
            return value;
        }

        /// <summary>
        /// Resizes the array to the specified length. New elements are default-initialized.
        /// </summary>
        public void Resize(int newLength)
        {
            if (newLength < 0)
                throw new ArgumentOutOfRangeException(nameof(newLength));

            int currentLength = Length;
            if (newLength == currentLength)
                return;

            if (newLength < currentLength)
            {
                RemoveRange(newLength, currentLength - newLength);
            }
            else
            {
                int toAdd = newLength - currentLength;
                // Use a heap-allocated array to avoid stackalloc scope issues
                T[] newElements = new T[Math.Min(toAdd, 256)];
                Array.Clear(newElements, 0, newElements.Length); // Ensure zeroed

                while (toAdd > 0)
                {
                    int chunk = Math.Min(toAdd, newElements.Length);
                    AppendRange(newElements.AsSpan(0, chunk));
                    toAdd -= chunk;
                }
            }
        }

        /// <summary>
        /// Ensures the array has at least the specified capacity. This may be useful before batch appends.
        /// </summary>
        /// <remarks>Currently this is a no-op as capacity management is handled internally.
        /// Future implementations may use this hint for pre-allocation.</remarks>
        public void EnsureCapacity(int minimumCapacity)
        {
            // Capacity management is handled internally by the memory allocator.
            // This method is provided for API consistency and future optimization.
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Converts this GhostSpan to a standard .NET array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            if (_sourceArray != null)
                return (T[])_sourceArray.Clone();

            return AsSpan().ToArray();
        }

        /// <summary>
        /// Implicit conversion to T[] (creates a copy).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T[](GhostSpan<T> value) => value.ToArray();

        /// <summary>
        /// Implicit conversion from T[] to GhostSpan.
        /// This creates a read-only GhostSpan that wraps the array safely.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GhostSpan<T>(T[]? value)
        {
            if (value == null || value.Length == 0)
                return default;

            return new GhostSpan<T>(value);
        }

        /// <summary>
        /// Implicit conversion to ReadOnlySpan&lt;T&gt;.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(GhostSpan<T> value) => value.AsSpan();

        /// <summary>
        /// Copies the elements to a destination span.
        /// </summary>
        public bool TryCopyTo(Span<T> destination)
        {
            return AsSpan().TryCopyTo(destination);
        }

        /// <summary>
        /// Copies the elements to a destination span.
        /// </summary>
        public void CopyTo(Span<T> destination)
        {
            AsSpan().CopyTo(destination);
        }

        // -------------------------------------------------------------------------
        // COMPARISON - EQUALS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this array equals the specified array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(T[]? other)
        {
            if (other is null) return IsEmpty;
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        /// <summary>
        /// Determines whether this array equals the specified GhostSpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostSpan<T> other)
        {
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        /// <summary>
        /// Determines whether this array equals the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<T> other)
        {
            return AsSpan().SequenceEqual(other);
        }

        // -------------------------------------------------------------------------
        // OPERATORS
        // -------------------------------------------------------------------------

        public static bool operator ==(GhostSpan<T> left, GhostSpan<T> right) => left.Equals(right);
        public static bool operator !=(GhostSpan<T> left, GhostSpan<T> right) => !left.Equals(right);
        public static bool operator ==(GhostSpan<T> left, T[]? right) => left.Equals(right);
        public static bool operator !=(GhostSpan<T> left, T[]? right) => !left.Equals(right);
        public static bool operator ==(T[]? left, GhostSpan<T> right) => right.Equals(left);
        public static bool operator !=(T[]? left, GhostSpan<T> right) => !right.Equals(left);

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the first occurrence of the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T value)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified value, starting at the specified index.
        /// </summary>
        public int IndexOf(T value, int startIndex)
        {
            var span = AsSpan();
            for (int i = startIndex; i < span.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified value within the specified range.
        /// </summary>
        public int IndexOf(T value, int startIndex, int count)
        {
            var span = AsSpan();
            int end = Math.Min(startIndex + count, span.Length);
            for (int i = startIndex; i < end; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<T> value)
        {
            if (value.IsEmpty)
                return 0;

            var span = AsSpan();
            if (value.Length > span.Length)
                return -1;

            for (int i = 0; i <= span.Length - value.Length; i++)
            {
                if (span.Slice(i, value.Length).SequenceEqual(value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(GhostSpan<T> value) => IndexOf(value.AsSpan());

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the last occurrence of the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(T value)
        {
            var span = AsSpan();
            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of the specified value, searching backward from the specified index.
        /// </summary>
        public int LastIndexOf(T value, int startIndex)
        {
            var span = AsSpan();
            for (int i = Math.Min(startIndex, span.Length - 1); i >= 0; i--)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(ReadOnlySpan<T> value)
        {
            if (value.IsEmpty)
                return Length;

            var span = AsSpan();
            if (value.Length > span.Length)
                return -1;

            for (int i = span.Length - value.Length; i >= 0; i--)
            {
                if (span.Slice(i, value.Length).SequenceEqual(value))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns the index of the last occurrence of the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(GhostSpan<T> value) => LastIndexOf(value.AsSpan());

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this array contains the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value) => IndexOf(value) >= 0;

        /// <summary>
        /// Determines whether this array contains the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ReadOnlySpan<T> value) => IndexOf(value) >= 0;

        /// <summary>
        /// Determines whether this array contains the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(GhostSpan<T> value) => IndexOf(value) >= 0;

        /// <summary>
        /// Determines whether this array contains all elements matching the predicate.
        /// </summary>
        public bool All(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (!predicate(span[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether this array contains any element matching the predicate.
        /// </summary>
        public bool Any(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Counts elements matching the predicate.
        /// </summary>
        public int Count(Func<T, bool> predicate)
        {
            var span = AsSpan();
            int count = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Counts occurrences of the specified value.
        /// </summary>
        public int Count(T value)
        {
            var span = AsSpan();
            int count = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[i], value))
                    count++;
            }
            return count;
        }

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this array starts with the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(T value)
        {
            var span = AsSpan();
            return span.Length > 0 && EqualityComparer<T>.Default.Equals(span[0], value);
        }

        /// <summary>
        /// Determines whether this array starts with the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(ReadOnlySpan<T> value)
        {
            var span = AsSpan();
            return span.Length >= value.Length && span.Slice(0, value.Length).SequenceEqual(value);
        }

        /// <summary>
        /// Determines whether this array starts with the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(GhostSpan<T> value) => StartsWith(value.AsSpan());

        /// <summary>
        /// Determines whether this array ends with the specified value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(T value)
        {
            var span = AsSpan();
            return span.Length > 0 && EqualityComparer<T>.Default.Equals(span[span.Length - 1], value);
        }

        /// <summary>
        /// Determines whether this array ends with the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(ReadOnlySpan<T> value)
        {
            var span = AsSpan();
            return span.Length >= value.Length && span.Slice(span.Length - value.Length).SequenceEqual(value);
        }

        /// <summary>
        /// Determines whether this array ends with the specified sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(GhostSpan<T> value) => EndsWith(value.AsSpan());

        // -------------------------------------------------------------------------
        // SLICING
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a slice of this array as a new GhostSpan.
        /// Note: For source arrays, this creates a new array (allocation).
        /// </summary>
        public GhostSpan<T> Slice(int start)
        {
            if (_sourceArray != null)
            {
                var slice = new T[_sourceArray.Length - start];
                Array.Copy(_sourceArray, start, slice, 0, slice.Length);
                return new GhostSpan<T>(slice);
            }

            return new GhostSpan<T>(_data.Slice(start * Unsafe.SizeOf<T>()));
        }

        /// <summary>
        /// Returns a slice of this array as a new GhostSpan.
        /// Note: For source arrays, this creates a new array (allocation).
        /// </summary>
        public GhostSpan<T> Slice(int start, int length)
        {
            if (_sourceArray != null)
            {
                var slice = new T[length];
                Array.Copy(_sourceArray, start, slice, 0, length);
                return new GhostSpan<T>(slice);
            }

            return new GhostSpan<T>(_data.Slice(start * Unsafe.SizeOf<T>(), length * Unsafe.SizeOf<T>()));
        }

        // -------------------------------------------------------------------------
        // ELEMENT ACCESS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Gets the first element of the array.
        /// </summary>
        public T First => Length > 0 ? AsSpan()[0] : throw new InvalidOperationException("Array is empty.");

        /// <summary>
        /// Gets the last element of the array.
        /// </summary>
        public T Last => Length > 0 ? AsSpan()[Length - 1] : throw new InvalidOperationException("Array is empty.");

        /// <summary>
        /// Gets the first element of the array, or a default value if empty.
        /// </summary>
        public T FirstOrDefault(T defaultValue = default) => Length > 0 ? AsSpan()[0] : defaultValue;

        /// <summary>
        /// Gets the last element of the array, or a default value if empty.
        /// </summary>
        public T LastOrDefault(T defaultValue = default) => Length > 0 ? AsSpan()[Length - 1] : defaultValue;

        /// <summary>
        /// Gets the element at the specified index, or a default value if out of bounds.
        /// </summary>
        public T ElementAtOrDefault(int index, T defaultValue = default)
        {
            var span = AsSpan();
            return (uint)index < (uint)span.Length ? span[index] : defaultValue;
        }

        /// <summary>
        /// Finds the first element matching the predicate.
        /// </summary>
        public T? Find(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    return span[i];
            }
            return default;
        }

        /// <summary>
        /// Finds the index of the first element matching the predicate.
        /// </summary>
        public int FindIndex(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Finds the last element matching the predicate.
        /// </summary>
        public T? FindLast(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (predicate(span[i]))
                    return span[i];
            }
            return default;
        }

        /// <summary>
        /// Finds the index of the last element matching the predicate.
        /// </summary>
        public int FindLastIndex(Func<T, bool> predicate)
        {
            var span = AsSpan();
            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (predicate(span[i]))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Finds all elements matching the predicate.
        /// </summary>
        public T[] FindAll(Func<T, bool> predicate)
        {
            var list = new List<T>();
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                    list.Add(span[i]);
            }
            return list.ToArray();
        }

        // -------------------------------------------------------------------------
        // TRANSFORMATION (Read-only operations returning new arrays)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reverses the order of elements in place.
        /// </summary>
        public void Reverse()
        {
            if (_sourceArray != null)
                ThrowReadOnly();

            AsWritableSpan().Reverse();
        }

        /// <summary>
        /// Returns a new array with elements in reverse order.
        /// </summary>
        public T[] ToReversedArray()
        {
            var result = ToArray();
            Array.Reverse(result);
            return result;
        }

        /// <summary>
        /// Fills all elements with the specified value.
        /// </summary>
        public void Fill(T value)
        {
            if (_sourceArray != null)
                ThrowReadOnly();

            AsWritableSpan().Fill(value);
        }

        /// <summary>
        /// Sorts the elements in place (requires IComparable).
        /// </summary>
        public void Sort()
        {
            if (_sourceArray != null)
                ThrowReadOnly();

            var span = AsWritableSpan();
            MemoryExtensions.Sort(span);
        }

        /// <summary>
        /// Sorts the elements in place using the specified comparer.
        /// </summary>
        public void Sort(IComparer<T> comparer)
        {
            if (_sourceArray != null)
                ThrowReadOnly();

            var span = AsWritableSpan();
            MemoryExtensions.Sort(span, comparer);
        }

        /// <summary>
        /// Returns a new sorted array.
        /// </summary>
        public T[] ToSortedArray()
        {
            var result = ToArray();
            Array.Sort(result);
            return result;
        }

        /// <summary>
        /// Returns a new sorted array using the specified comparer.
        /// </summary>
        public T[] ToSortedArray(IComparer<T> comparer)
        {
            var result = ToArray();
            Array.Sort(result, comparer);
            return result;
        }

        /// <summary>
        /// Returns distinct elements as a new array.
        /// </summary>
        public T[] Distinct()
        {
            var seen = new HashSet<T>();
            var result = new List<T>();
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                if (seen.Add(span[i]))
                    result.Add(span[i]);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Projects each element using the specified selector.
        /// </summary>
        public TResult[] Select<TResult>(Func<T, TResult> selector)
        {
            var span = AsSpan();
            var result = new TResult[span.Length];
            for (int i = 0; i < span.Length; i++)
            {
                result[i] = selector(span[i]);
            }
            return result;
        }

        /// <summary>
        /// Filters elements using the specified predicate.
        /// </summary>
        public T[] Where(Func<T, bool> predicate)
        {
            return FindAll(predicate);
        }

        /// <summary>
        /// Takes the first n elements.
        /// </summary>
        public T[] Take(int count)
        {
            var span = AsSpan();
            count = Math.Min(count, span.Length);
            return span.Slice(0, count).ToArray();
        }

        /// <summary>
        /// Skips the first n elements.
        /// </summary>
        public T[] Skip(int count)
        {
            var span = AsSpan();
            if (count >= span.Length)
                return [];
            return span.Slice(count).ToArray();
        }

        // -------------------------------------------------------------------------
        // BINARY SEARCH (for sorted arrays)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Performs a binary search for the specified value (array must be sorted).
        /// Uses the default comparer for T.
        /// </summary>
        /// <returns>The index of the value if found; otherwise, a negative number that is the bitwise complement of the index of the next larger element.</returns>
        public int BinarySearch(T value, IComparer<T>? comparer = null)
        {
            var span = AsSpan();
            comparer ??= Comparer<T>.Default;
            int lo = 0;
            int hi = span.Length - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int cmp = comparer.Compare(span[mid], value);

                if (cmp == 0)
                    return mid;
                if (cmp < 0)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return ~lo;
        }

        /// <summary>
        /// Returns the hash code for this array.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            var span = AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                hash.Add(span[i]);
            }
            return hash.ToHashCode();
        }

        // -------------------------------------------------------------------------
        // STRING REPRESENTATION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a string representation of the array.
        /// </summary>
        public override string ToString()
        {
            var span = AsSpan();
            if (span.IsEmpty)
                return "[]";

            return $"[{string.Join(", ", ToArray())}]";
        }

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns an enumerator that iterates through the elements.
        /// </summary>
        public ReadOnlySpan<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        // -------------------------------------------------------------------------
        // HELPER METHODS
        // -------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReadOnly() => throw new InvalidOperationException("This GhostSpan instance is read-only.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowEmptyArray() => throw new InvalidOperationException("Array is empty.");

        // -------------------------------------------------------------------------
        // OBJECT OVERRIDES (ref struct cannot inherit, but we provide these)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Not supported for ref struct. Use Equals(GhostSpan&lt;T&gt;) or Equals(T[]) instead.
        /// </summary>
        public override bool Equals(object? obj) => throw new NotSupportedException("Cannot box a ref struct. Use Equals(GhostSpan<T>) or Equals(T[]) instead.");
    }
}
