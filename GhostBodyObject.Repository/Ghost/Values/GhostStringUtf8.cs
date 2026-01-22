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
using System.Text;

namespace GhostBodyObject.Repository
{
    /// <summary>
    /// A ref struct that wraps UTF-8 string data stored in pinned memory, providing string-like semantics
    /// with efficient memory access. Designed to integrate seamlessly with the standard <see cref="string"/> class
    /// and <see cref="GhostStringUtf16"/>.
    /// </summary>
    public ref struct GhostStringUtf8
    {
        private readonly BodyBase _body;
        private readonly PinnedMemory<byte> _data;
        private readonly int _arrayIndex;

        // When created from a string, we store the UTF-8 bytes directly
        private readonly byte[]? _sourceBytes;

        // -------------------------------------------------------------------------
        // CONSTRUCTORS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of GhostStringUtf8 with the specified body, array index, and pinned memory data.
        /// </summary>
        public GhostStringUtf8(BodyBase body, int arrayIndex, PinnedMemory<byte> data)
        {
            _body = body;
            _arrayIndex = arrayIndex;
            _data = data;
            _sourceBytes = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostStringUtf8 with only pinned memory data (read-only access).
        /// </summary>
        public GhostStringUtf8(PinnedMemory<byte> data)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = data;
            _sourceBytes = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostStringUtf8 from UTF-8 bytes (read-only, used for implicit conversion).
        /// </summary>
        private GhostStringUtf8(byte[] sourceBytes)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = default;
            _sourceBytes = sourceBytes;
        }

        // -------------------------------------------------------------------------
        // PROPERTIES
        // -------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the string in UTF-8 bytes.
        /// </summary>
        public int ByteLength => _sourceBytes != null ? _sourceBytes.Length : _data.Length;

        /// <summary>
        /// Gets the length of the string in characters (requires decoding).
        /// </summary>
        public int Length {
            get {
                var bytes = AsBytes();
                return bytes.IsEmpty ? 0 : Encoding.UTF8.GetCharCount(bytes);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this string is empty.
        /// </summary>
        public bool IsEmpty => ByteLength == 0;

        /// <summary>
        /// Gets a value indicating whether this string is null or empty.
        /// </summary>
        public bool IsNullOrEmpty => _sourceBytes != null ? _sourceBytes.Length == 0 : _data.IsEmpty;

        /// <summary>
        /// Gets the byte at the specified index.
        /// </summary>
        public byte this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if ((uint)index >= (uint)ByteLength)
                    ThrowIndexOutOfRange();
                return AsBytes()[index];
            }
        }

        // -------------------------------------------------------------------------
        // SPAN ACCESS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the string data as a ReadOnlySpan of bytes (UTF-8 encoded).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsBytes()
        {
            if (_sourceBytes != null)
                return _sourceBytes.AsSpan();

            return _data.Span;
        }

        /// <summary>
        /// Returns the string data as a Span of bytes (for modification).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the source is read-only.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsWritableBytes()
        {
            if (_sourceBytes != null)
                ThrowReadOnly();

            return _data.Span;
        }

        /// <summary>
        /// Returns a portion of the UTF-8 bytes as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsBytes(int start) => AsBytes().Slice(start);

        /// <summary>
        /// Returns a portion of the UTF-8 bytes as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsBytes(int start, int length) => AsBytes().Slice(start, length);

        /// <summary>
        /// Returns the string data as a ReadOnlySpan of characters (decoded from UTF-8).
        /// Note: This allocates a char array for the decoded characters.
        /// </summary>
        public ReadOnlySpan<char> AsSpan()
        {
            var bytes = AsBytes();
            if (bytes.IsEmpty)
                return ReadOnlySpan<char>.Empty;

            var charCount = Encoding.UTF8.GetCharCount(bytes);
            var chars = new char[charCount];
            Encoding.UTF8.GetChars(bytes, chars);
            return chars.AsSpan();
        }

        /// <summary>
        /// Decodes the UTF-8 bytes into the provided character buffer.
        /// </summary>
        /// <returns>The number of characters written.</returns>
        public int DecodeInto(Span<char> destination)
        {
            var bytes = AsBytes();
            if (bytes.IsEmpty)
                return 0;

            return Encoding.UTF8.GetChars(bytes, destination);
        }

        /// <summary>
        /// Gets the required character buffer size to decode this UTF-8 string.
        /// </summary>
        public int GetCharCount() => Length;

        // -------------------------------------------------------------------------
        // STRING MODIFICATION (Writes back to entity body)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Sets the string value, updating the underlying entity body.
        /// </summary>
        public unsafe void SetString(string value)
        {
            if (_body == null)
                ThrowReadOnly();

            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            BodyBase.SwapAnyArray(_body, utf8Bytes.AsSpan(), _arrayIndex);
        }

        /// <summary>
        /// Sets the string value from another GhostStringUtf8.
        /// </summary>
        public unsafe void SetString(GhostStringUtf8 value)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, value.AsBytes(), _arrayIndex);
        }

        /// <summary>
        /// Sets the string value from a GhostStringUtf16 (converts to UTF-8).
        /// </summary>
        public unsafe void SetString(GhostStringUtf16 value)
        {
            if (_body == null)
                ThrowReadOnly();

            var utf8Bytes = Encoding.UTF8.GetBytes(value.AsSpan().ToArray());
            BodyBase.SwapAnyArray(_body, utf8Bytes.AsSpan(), _arrayIndex);
        }

        /// <summary>
        /// Sets the string value from a ReadOnlySpan of bytes (must be valid UTF-8).
        /// </summary>
        public unsafe void SetBytes(ReadOnlySpan<byte> value)
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, value, _arrayIndex);
        }

        // -------------------------------------------------------------------------
        // HIGH-PERFORMANCE STRING MODIFICATION - IN-PLACE OPERATIONS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Appends a string to the end (UTF-8 encoded).
        /// </summary>
        public unsafe void Append(string value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (string.IsNullOrEmpty(value))
                return;

            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            BodyBase.AppendToArray(_body, utf8Bytes.AsSpan(), _arrayIndex);
        }

        /// <summary>
        /// Appends UTF-8 bytes to the end.
        /// </summary>
        public unsafe void Append(ReadOnlySpan<byte> value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (value.IsEmpty)
                return;

            BodyBase.AppendToArray(_body, value, _arrayIndex);
        }

        /// <summary>
        /// Appends another GhostStringUtf8 to the end.
        /// </summary>
        public void Append(GhostStringUtf8 value)
        {
            Append(value.AsBytes());
        }

        /// <summary>
        /// Appends a GhostStringUtf16 to the end (converts to UTF-8).
        /// </summary>
        public void Append(GhostStringUtf16 value)
        {
            Append(value.ToString());
        }

        /// <summary>
        /// Prepends a string to the beginning (UTF-8 encoded).
        /// </summary>
        public unsafe void Prepend(string value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (string.IsNullOrEmpty(value))
                return;

            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            BodyBase.PrependToArray(_body, utf8Bytes.AsSpan(), _arrayIndex);
        }

        /// <summary>
        /// Prepends UTF-8 bytes to the beginning.
        /// </summary>
        public unsafe void Prepend(ReadOnlySpan<byte> value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (value.IsEmpty)
                return;

            BodyBase.PrependToArray(_body, value, _arrayIndex);
        }

        /// <summary>
        /// Prepends another GhostStringUtf8 to the beginning.
        /// </summary>
        public void Prepend(GhostStringUtf8 value)
        {
            Prepend(value.AsBytes());
        }

        /// <summary>
        /// Prepends a GhostStringUtf16 to the beginning (converts to UTF-8).
        /// </summary>
        public void Prepend(GhostStringUtf16 value)
        {
            Prepend(value.ToString());
        }

        /// <summary>
        /// Inserts a string at the specified byte offset (UTF-8 encoded).
        /// </summary>
        public unsafe void InsertBytesAt(int byteOffset, string value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (string.IsNullOrEmpty(value))
                return;
            if ((uint)byteOffset > (uint)ByteLength)
                ThrowIndexOutOfRange();

            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            BodyBase.InsertIntoArray(_body, utf8Bytes.AsSpan(), _arrayIndex, byteOffset);
        }

        /// <summary>
        /// Inserts UTF-8 bytes at the specified byte offset.
        /// </summary>
        public unsafe void InsertBytesAt(int byteOffset, ReadOnlySpan<byte> value)
        {
            if (_body == null)
                ThrowReadOnly();
            if (value.IsEmpty)
                return;
            if ((uint)byteOffset > (uint)ByteLength)
                ThrowIndexOutOfRange();

            BodyBase.InsertIntoArray(_body, value, _arrayIndex, byteOffset);
        }

        /// <summary>
        /// Removes bytes starting at the specified byte offset.
        /// </summary>
        public unsafe void RemoveBytesAt(int byteOffset, int byteCount)
        {
            if (_body == null)
                ThrowReadOnly();
            if (byteCount <= 0)
                return;
            if (byteOffset < 0 || (uint)(byteOffset + byteCount) > (uint)ByteLength)
                ThrowIndexOutOfRange();

            BodyBase.RemoveFromArray(_body, _arrayIndex, byteOffset, byteCount);
        }

        /// <summary>
        /// Removes all bytes from the specified offset to the end.
        /// </summary>
        public void RemoveBytesFrom(int byteOffset)
        {
            RemoveBytesAt(byteOffset, ByteLength - byteOffset);
        }

        /// <summary>
        /// Removes the first occurrence of the specified byte sequence.
        /// </summary>
        /// <returns>True if the sequence was found and removed; otherwise, false.</returns>
        public bool RemoveFirstBytes(ReadOnlySpan<byte> value)
        {
            int index = IndexOfBytes(value);
            if (index < 0)
                return false;
            RemoveBytesAt(index, value.Length);
            return true;
        }

        /// <summary>
        /// Removes all occurrences of the specified byte sequence.
        /// </summary>
        /// <returns>The number of occurrences removed.</returns>
        public int RemoveAllBytes(ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
                return 0;
            int count = 0;
            int currentByteLength = ByteLength;
            int index;
            while (currentByteLength >= value.Length && (index = AsBytes(0, currentByteLength).IndexOf(value)) >= 0)
            {
                RemoveBytesAt(index, value.Length);
                currentByteLength -= value.Length;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Replaces a range of bytes with new bytes.
        /// </summary>
        public unsafe void ReplaceBytesRange(int byteOffset, int byteCount, ReadOnlySpan<byte> replacement)
        {
            if (_body == null)
                ThrowReadOnly();
            if (byteOffset < 0 || byteCount < 0 || (uint)(byteOffset + byteCount) > (uint)ByteLength)
                ThrowIndexOutOfRange();

            BodyBase.ReplaceInArray(_body, replacement, _arrayIndex, byteOffset, byteCount);
        }

        /// <summary>
        /// Replaces a range of bytes with a UTF-8 encoded string.
        /// </summary>
        public void ReplaceBytesRange(int byteOffset, int byteCount, string replacement)
        {
            var utf8Bytes = string.IsNullOrEmpty(replacement)
                ? ReadOnlySpan<byte>.Empty
                : Encoding.UTF8.GetBytes(replacement).AsSpan();
            ReplaceBytesRange(byteOffset, byteCount, utf8Bytes);
        }

        /// <summary>
        /// Replaces the first occurrence of a byte sequence with another.
        /// </summary>
        /// <returns>True if a replacement was made; otherwise, false.</returns>
        public bool ReplaceFirstBytes(ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
        {
            if (oldValue.IsEmpty)
                return false;
            int index = IndexOfBytes(oldValue);
            if (index < 0)
                return false;
            ReplaceBytesRange(index, oldValue.Length, newValue);
            return true;
        }

        /// <summary>
        /// Replaces all occurrences of a byte sequence in place.
        /// </summary>
        /// <returns>The number of replacements made.</returns>
        public int ReplaceAllBytesInPlace(ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
        {
            if (oldValue.IsEmpty)
                return 0;
            int count = 0;
            int searchStart = 0;
            int relativeIndex;
            while ((relativeIndex = AsBytes(searchStart).IndexOf(oldValue)) >= 0)
            {
                int absoluteIndex = searchStart + relativeIndex;
                ReplaceBytesRange(absoluteIndex, oldValue.Length, newValue);
                searchStart = absoluteIndex + newValue.Length;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Clears the string (sets length to 0).
        /// </summary>
        public unsafe void Clear()
        {
            if (_body == null)
                ThrowReadOnly();

            BodyBase.SwapAnyArray(_body, ReadOnlySpan<byte>.Empty, _arrayIndex);
        }

        /// <summary>
        /// Trims ASCII whitespace bytes from both ends in place.
        /// </summary>
        public void TrimAsciiInPlace()
        {
            var bytes = AsBytes();
            int start = 0;
            int end = bytes.Length;

            while (start < end && IsAsciiWhitespace(bytes[start]))
                start++;
            while (end > start && IsAsciiWhitespace(bytes[end - 1]))
                end--;

            if (start == 0 && end == bytes.Length)
                return; // Nothing to trim

            if (start > 0 || end < bytes.Length)
            {
                var trimmed = bytes.Slice(start, end - start);
                SetBytes(trimmed);
            }
        }

        /// <summary>
        /// Trims ASCII whitespace from the start in place.
        /// </summary>
        public void TrimAsciiStartInPlace()
        {
            var bytes = AsBytes();
            int start = 0;

            while (start < bytes.Length && IsAsciiWhitespace(bytes[start]))
                start++;

            if (start > 0)
                RemoveBytesAt(0, start);
        }

        /// <summary>
        /// Trims ASCII whitespace from the end in place.
        /// </summary>
        public void TrimAsciiEndInPlace()
        {
            var bytes = AsBytes();
            int end = bytes.Length;

            while (end > 0 && IsAsciiWhitespace(bytes[end - 1]))
                end--;

            if (end < bytes.Length)
                RemoveBytesFrom(end);
        }

        /// <summary>
        /// Converts ASCII characters to uppercase in place.
        /// </summary>
        public void ToUpperAsciiInPlace()
        {
            if (_sourceBytes != null)
                ThrowReadOnly();

            var bytes = AsWritableBytes();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] >= (byte)'a' && bytes[i] <= (byte)'z')
                    bytes[i] = (byte)(bytes[i] - 32);
            }
        }

        /// <summary>
        /// Converts ASCII characters to lowercase in place.
        /// </summary>
        public void ToLowerAsciiInPlace()
        {
            if (_sourceBytes != null)
                ThrowReadOnly();

            var bytes = AsWritableBytes();
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] >= (byte)'A' && bytes[i] <= (byte)'Z')
                    bytes[i] = (byte)(bytes[i] + 32);
            }
        }

        /// <summary>
        /// Reverses the UTF-8 bytes in place.
        /// Warning: This may produce invalid UTF-8 if the string contains multi-byte characters.
        /// Use only for ASCII strings or when you need raw byte reversal.
        /// </summary>
        public void ReverseBytesInPlace()
        {
            if (_sourceBytes != null)
                ThrowReadOnly();

            AsWritableBytes().Reverse();
        }

        private static bool IsAsciiWhitespace(byte b)
        {
            return b == (byte)' ' || b == (byte)'\t' || b == (byte)'\n' ||
                   b == (byte)'\r' || b == (byte)'\v' || b == (byte)'\f';
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Converts this GhostStringUtf8 to a standard .NET string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            var bytes = AsBytes();
            if (bytes.IsEmpty)
                return string.Empty;

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Converts this GhostStringUtf8 to a GhostStringUtf16.
        /// Note: This allocates memory for the UTF-16 representation.
        /// </summary>
        public GhostStringUtf16 ToUtf16()
        {
            return ToString();
        }

        /// <summary>
        /// Implicit conversion to string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(GhostStringUtf8 value) => value.ToString();

        /// <summary>
        /// Implicit conversion from string to GhostStringUtf8.
        /// This creates a read-only GhostStringUtf8 that contains the UTF-8 encoded bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GhostStringUtf8(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return default;

            var utf8Bytes = Encoding.UTF8.GetBytes(value);
            return new GhostStringUtf8(utf8Bytes);
        }

        /// <summary>
        /// Implicit conversion to ReadOnlySpan&lt;byte&gt;.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(GhostStringUtf8 value) => value.AsBytes();

        /// <summary>
        /// Explicit conversion from GhostStringUtf16 to GhostStringUtf8.
        /// </summary>
        public static explicit operator GhostStringUtf8(GhostStringUtf16 value)
        {
            if (value.IsEmpty)
                return default;

            var utf8Bytes = value.ToUtf8Bytes();
            return new GhostStringUtf8(utf8Bytes);
        }

        /// <summary>
        /// Returns the raw UTF-8 bytes as an array.
        /// </summary>
        public byte[] ToArray()
        {
            return AsBytes().ToArray();
        }

        /// <summary>
        /// Converts to UTF-16 byte array.
        /// </summary>
        public byte[] ToUtf16Bytes()
        {
            var str = ToString();
            return MemoryMarshal.AsBytes(str.AsSpan()).ToArray();
        }

        /// <summary>
        /// Copies the UTF-8 bytes to a destination span.
        /// </summary>
        public bool TryCopyTo(Span<byte> destination)
        {
            return AsBytes().TryCopyTo(destination);
        }

        /// <summary>
        /// Copies the UTF-8 bytes to a destination span.
        /// </summary>
        public void CopyTo(Span<byte> destination)
        {
            AsBytes().CopyTo(destination);
        }

        // -------------------------------------------------------------------------
        // COMPARISON - EQUALS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this string equals the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(string? other)
        {
            if (other is null) return IsEmpty;
            var otherBytes = Encoding.UTF8.GetBytes(other);
            return AsBytes().SequenceEqual(otherBytes);
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostStringUtf8 other)
        {
            return AsBytes().SequenceEqual(other.AsBytes());
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostStringUtf16 other)
        {
            return AsBytes().SequenceEqual(other.ToUtf8Bytes());
        }

        /// <summary>
        /// Determines whether this string equals the specified byte span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<byte> other)
        {
            return AsBytes().SequenceEqual(other);
        }

        /// <summary>
        /// Determines whether this string equals the specified string using the specified comparison.
        /// </summary>
        public bool Equals(string? other, StringComparison comparisonType)
        {
            if (other is null) return IsEmpty;
            return ToString().Equals(other, comparisonType);
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf8 using the specified comparison.
        /// </summary>
        public bool Equals(GhostStringUtf8 other, StringComparison comparisonType)
        {
            return ToString().Equals(other.ToString(), comparisonType);
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool Equals(GhostStringUtf16 other, StringComparison comparisonType)
        {
            return ToString().Equals(other.ToString(), comparisonType);
        }

        // -------------------------------------------------------------------------
        // COMPARISON - COMPARE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Compares this string to another string.
        /// </summary>
        public int CompareTo(string? other)
        {
            if (other is null) return IsEmpty ? 0 : 1;
            return string.Compare(ToString(), other, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf8.
        /// </summary>
        public int CompareTo(GhostStringUtf8 other)
        {
            return AsBytes().SequenceCompareTo(other.AsBytes());
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf16.
        /// </summary>
        public int CompareTo(GhostStringUtf16 other)
        {
            return string.Compare(ToString(), other.ToString(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this string to another string using the specified comparison.
        /// </summary>
        public int CompareTo(string? other, StringComparison comparisonType)
        {
            if (other is null) return IsEmpty ? 0 : 1;
            return string.Compare(ToString(), other, comparisonType);
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf8 using the specified comparison.
        /// </summary>
        public int CompareTo(GhostStringUtf8 other, StringComparison comparisonType)
        {
            return string.Compare(ToString(), other.ToString(), comparisonType);
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf16 using the specified comparison.
        /// </summary>
        public int CompareTo(GhostStringUtf16 other, StringComparison comparisonType)
        {
            return string.Compare(ToString(), other.ToString(), comparisonType);
        }

        // -------------------------------------------------------------------------
        // OPERATORS
        // -------------------------------------------------------------------------

        public static bool operator ==(GhostStringUtf8 left, GhostStringUtf8 right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf8 left, GhostStringUtf8 right) => !left.Equals(right);
        public static bool operator ==(GhostStringUtf8 left, string? right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf8 left, string? right) => !left.Equals(right);
        public static bool operator ==(string? left, GhostStringUtf8 right) => right.Equals(left);
        public static bool operator !=(string? left, GhostStringUtf8 right) => !right.Equals(left);
        public static bool operator ==(GhostStringUtf8 left, GhostStringUtf16 right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf8 left, GhostStringUtf16 right) => !left.Equals(right);
        public static bool operator ==(GhostStringUtf16 left, GhostStringUtf8 right) => right.Equals(left);
        public static bool operator !=(GhostStringUtf16 left, GhostStringUtf8 right) => !right.Equals(left);
        public static bool operator ==(GhostStringUtf8 left, ReadOnlySpan<byte> right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf8 left, ReadOnlySpan<byte> right) => !left.Equals(right);

        public static bool operator <(GhostStringUtf8 left, GhostStringUtf8 right) => left.CompareTo(right) < 0;
        public static bool operator >(GhostStringUtf8 left, GhostStringUtf8 right) => left.CompareTo(right) > 0;
        public static bool operator <=(GhostStringUtf8 left, GhostStringUtf8 right) => left.CompareTo(right) <= 0;
        public static bool operator >=(GhostStringUtf8 left, GhostStringUtf8 right) => left.CompareTo(right) >= 0;

        public static bool operator <(GhostStringUtf8 left, string? right) => left.CompareTo(right) < 0;
        public static bool operator >(GhostStringUtf8 left, string? right) => left.CompareTo(right) > 0;
        public static bool operator <=(GhostStringUtf8 left, string? right) => left.CompareTo(right) <= 0;
        public static bool operator >=(GhostStringUtf8 left, string? right) => left.CompareTo(right) >= 0;

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF (operates on decoded string)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the first occurrence of the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(char value) => ToString().IndexOf(value);

        /// <summary>
        /// Returns the index of the first occurrence of the specified character, starting at the specified index.
        /// </summary>
        public int IndexOf(char value, int startIndex) => ToString().IndexOf(value, startIndex);

        /// <summary>
        /// Returns the index of the first occurrence of the specified character within the specified range.
        /// </summary>
        public int IndexOf(char value, int startIndex, int count) => ToString().IndexOf(value, startIndex, count);

        /// <summary>
        /// Returns the index of the first occurrence of the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(string value) => ToString().IndexOf(value);

        /// <summary>
        /// Returns the index of the first occurrence of the specified string using the specified comparison.
        /// </summary>
        public int IndexOf(string value, StringComparison comparisonType) => ToString().IndexOf(value, comparisonType);

        /// <summary>
        /// Returns the index of the first occurrence of the specified string, starting at the specified index.
        /// </summary>
        public int IndexOf(string value, int startIndex) => ToString().IndexOf(value, startIndex);

        /// <summary>
        /// Returns the index of the first occurrence of the specified string, starting at the specified index, using the specified comparison.
        /// </summary>
        public int IndexOf(string value, int startIndex, StringComparison comparisonType) => ToString().IndexOf(value, startIndex, comparisonType);

        /// <summary>
        /// Returns the index of the first occurrence of the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(GhostStringUtf8 value) => ToString().IndexOf(value.ToString());

        /// <summary>
        /// Returns the index of the first occurrence of the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(GhostStringUtf16 value) => ToString().IndexOf(value.ToString());

        /// <summary>
        /// Returns the index of the first occurrence of any of the specified characters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfAny(ReadOnlySpan<char> anyOf) => ToString().AsSpan().IndexOfAny(anyOf);

        /// <summary>
        /// Returns the index of the first occurrence of any of the specified characters.
        /// </summary>
        public int IndexOfAny(params char[] anyOf) => ToString().IndexOfAny(anyOf);

        // -------------------------------------------------------------------------
        // SEARCH - BYTE INDEXOF (operates on raw UTF-8 bytes)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the byte index of the first occurrence of the specified byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfByte(byte value) => AsBytes().IndexOf(value);

        /// <summary>
        /// Returns the byte index of the first occurrence of the specified byte sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfBytes(ReadOnlySpan<byte> value) => AsBytes().IndexOf(value);

        /// <summary>
        /// Returns the byte index of the first occurrence of any of the specified bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfAnyByte(ReadOnlySpan<byte> anyOf) => AsBytes().IndexOfAny(anyOf);

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the last occurrence of the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(char value) => ToString().LastIndexOf(value);

        /// <summary>
        /// Returns the index of the last occurrence of the specified character, searching backward from the specified index.
        /// </summary>
        public int LastIndexOf(char value, int startIndex) => ToString().LastIndexOf(value, startIndex);

        /// <summary>
        /// Returns the index of the last occurrence of the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(string value) => ToString().LastIndexOf(value);

        /// <summary>
        /// Returns the index of the last occurrence of the specified string using the specified comparison.
        /// </summary>
        public int LastIndexOf(string value, StringComparison comparisonType) => ToString().LastIndexOf(value, comparisonType);

        /// <summary>
        /// Returns the index of the last occurrence of the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(GhostStringUtf8 value) => ToString().LastIndexOf(value.ToString());

        /// <summary>
        /// Returns the index of the last occurrence of the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(GhostStringUtf16 value) => ToString().LastIndexOf(value.ToString());

        /// <summary>
        /// Returns the index of the last occurrence of any of the specified characters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOfAny(ReadOnlySpan<char> anyOf) => ToString().AsSpan().LastIndexOfAny(anyOf);

        /// <summary>
        /// Returns the index of the last occurrence of any of the specified characters.
        /// </summary>
        public int LastIndexOfAny(params char[] anyOf) => ToString().LastIndexOfAny(anyOf);

        /// <summary>
        /// Returns the byte index of the last occurrence of the specified byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOfByte(byte value) => AsBytes().LastIndexOf(value);

        /// <summary>
        /// Returns the byte index of the last occurrence of the specified byte sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOfBytes(ReadOnlySpan<byte> value) => AsBytes().LastIndexOf(value);

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this string contains the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(char value) => ToString().Contains(value);

        /// <summary>
        /// Determines whether this string contains the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string value) => ToString().Contains(value, StringComparison.Ordinal);

        /// <summary>
        /// Determines whether this string contains the specified string using the specified comparison.
        /// </summary>
        public bool Contains(string value, StringComparison comparisonType) => ToString().Contains(value, comparisonType);

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(GhostStringUtf8 value) => AsBytes().IndexOf(value.AsBytes()) >= 0;

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf8 using the specified comparison.
        /// </summary>
        public bool Contains(GhostStringUtf8 value, StringComparison comparisonType) => ToString().Contains(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(GhostStringUtf16 value) => ToString().Contains(value.ToString(), StringComparison.Ordinal);

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool Contains(GhostStringUtf16 value, StringComparison comparisonType) => ToString().Contains(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string contains the specified byte sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsBytes(ReadOnlySpan<byte> value) => AsBytes().IndexOf(value) >= 0;

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this string starts with the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(char value)
        {
            if (IsEmpty) return false;
            // For ASCII, we can check directly
            if (value < 128)
            {
                var bytes = AsBytes();
                return bytes.Length > 0 && bytes[0] == value;
            }
            return ToString().StartsWith(value);
        }

        /// <summary>
        /// Determines whether this string starts with the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) => ToString().StartsWith(value);

        /// <summary>
        /// Determines whether this string starts with the specified string using the specified comparison.
        /// </summary>
        public bool StartsWith(string value, StringComparison comparisonType) => ToString().StartsWith(value, comparisonType);

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(GhostStringUtf8 value) => AsBytes().StartsWith(value.AsBytes());

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf8 using the specified comparison.
        /// </summary>
        public bool StartsWith(GhostStringUtf8 value, StringComparison comparisonType) => ToString().StartsWith(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(GhostStringUtf16 value) => ToString().StartsWith(value.ToString());

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool StartsWith(GhostStringUtf16 value, StringComparison comparisonType) => ToString().StartsWith(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string starts with the specified byte sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWithBytes(ReadOnlySpan<byte> value) => AsBytes().StartsWith(value);

        /// <summary>
        /// Determines whether this string ends with the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(char value)
        {
            if (IsEmpty) return false;
            // For ASCII, we can check directly (if last byte is ASCII)
            if (value < 128)
            {
                var bytes = AsBytes();
                return bytes.Length > 0 && bytes[bytes.Length - 1] == value;
            }
            return ToString().EndsWith(value);
        }

        /// <summary>
        /// Determines whether this string ends with the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(string value) => ToString().EndsWith(value);

        /// <summary>
        /// Determines whether this string ends with the specified string using the specified comparison.
        /// </summary>
        public bool EndsWith(string value, StringComparison comparisonType) => ToString().EndsWith(value, comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf8.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(GhostStringUtf8 value) => AsBytes().EndsWith(value.AsBytes());

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf8 using the specified comparison.
        /// </summary>
        public bool EndsWith(GhostStringUtf8 value, StringComparison comparisonType) => ToString().EndsWith(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(GhostStringUtf16 value) => ToString().EndsWith(value.ToString());

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool EndsWith(GhostStringUtf16 value, StringComparison comparisonType) => ToString().EndsWith(value.ToString(), comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified byte sequence.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWithBytes(ReadOnlySpan<byte> value) => AsBytes().EndsWith(value);

        // -------------------------------------------------------------------------
        // SUBSTRING / SLICE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a substring starting at the specified character index.
        /// </summary>
        public string Substring(int startIndex) => ToString().Substring(startIndex);

        /// <summary>
        /// Returns a substring of the specified length starting at the specified character index.
        /// </summary>
        public string Substring(int startIndex, int length) => ToString().Substring(startIndex, length);

        /// <summary>
        /// Returns a slice of this string's UTF-8 bytes as a new GhostStringUtf8.
        /// Note: This creates a new byte array for the slice.
        /// </summary>
        public GhostStringUtf8 SliceBytes(int start)
        {
            var slicedBytes = AsBytes(start).ToArray();
            return new GhostStringUtf8(slicedBytes);
        }

        /// <summary>
        /// Returns a slice of this string's UTF-8 bytes as a new GhostStringUtf8.
        /// Note: This creates a new byte array for the slice.
        /// </summary>
        public GhostStringUtf8 SliceBytes(int start, int length)
        {
            var slicedBytes = AsBytes(start, length).ToArray();
            return new GhostStringUtf8(slicedBytes);
        }

        // -------------------------------------------------------------------------
        // TRIM
        // -------------------------------------------------------------------------

        /// <summary>
        /// Removes all leading and trailing white-space characters and returns the result as a string.
        /// </summary>
        public string Trim() => ToString().Trim();

        /// <summary>
        /// Removes all leading and trailing occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string Trim(char trimChar) => ToString().Trim(trimChar);

        /// <summary>
        /// Removes all leading and trailing occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string Trim(params char[] trimChars) => ToString().Trim(trimChars);

        /// <summary>
        /// Removes all leading white-space characters and returns the result as a string.
        /// </summary>
        public string TrimStart() => ToString().TrimStart();

        /// <summary>
        /// Removes all leading occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string TrimStart(char trimChar) => ToString().TrimStart(trimChar);

        /// <summary>
        /// Removes all leading occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string TrimStart(params char[] trimChars) => ToString().TrimStart(trimChars);

        /// <summary>
        /// Removes all trailing white-space characters and returns the result as a string.
        /// </summary>
        public string TrimEnd() => ToString().TrimEnd();

        /// <summary>
        /// Removes all trailing occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string TrimEnd(char trimChar) => ToString().TrimEnd(trimChar);

        /// <summary>
        /// Removes all trailing occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string TrimEnd(params char[] trimChars) => ToString().TrimEnd(trimChars);

        // -------------------------------------------------------------------------
        // CASE CONVERSION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a copy of this string converted to uppercase.
        /// </summary>
        public string ToUpper() => ToString().ToUpperInvariant();

        /// <summary>
        /// Returns a copy of this string converted to uppercase using the specified culture.
        /// </summary>
        public string ToUpper(System.Globalization.CultureInfo culture) => ToString().ToUpper(culture);

        /// <summary>
        /// Returns a copy of this string converted to uppercase using the invariant culture.
        /// </summary>
        public string ToUpperInvariant() => ToString().ToUpperInvariant();

        /// <summary>
        /// Returns a copy of this string converted to lowercase.
        /// </summary>
        public string ToLower() => ToString().ToLowerInvariant();

        /// <summary>
        /// Returns a copy of this string converted to lowercase using the specified culture.
        /// </summary>
        public string ToLower(System.Globalization.CultureInfo culture) => ToString().ToLower(culture);

        /// <summary>
        /// Returns a copy of this string converted to lowercase using the invariant culture.
        /// </summary>
        public string ToLowerInvariant() => ToString().ToLowerInvariant();

        // -------------------------------------------------------------------------
        // REPLACE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Replaces all occurrences of a specified character with another character.
        /// </summary>
        public string Replace(char oldChar, char newChar) => ToString().Replace(oldChar, newChar);

        /// <summary>
        /// Replaces all occurrences of a specified string with another string.
        /// </summary>
        public string Replace(string oldValue, string? newValue) => ToString().Replace(oldValue, newValue);

        /// <summary>
        /// Replaces all occurrences of a specified string with another string using the specified comparison.
        /// </summary>
        public string Replace(string oldValue, string? newValue, StringComparison comparisonType)
            => ToString().Replace(oldValue, newValue, comparisonType);

        // -------------------------------------------------------------------------
        // SPLIT
        // -------------------------------------------------------------------------

        /// <summary>
        /// Splits the string by the specified separator.
        /// </summary>
        public string[] Split(char separator, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, options);

        /// <summary>
        /// Splits the string by the specified separators.
        /// </summary>
        public string[] Split(char[] separator, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, options);

        /// <summary>
        /// Splits the string by the specified separator.
        /// </summary>
        public string[] Split(string separator, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, options);

        /// <summary>
        /// Splits the string by the specified separators.
        /// </summary>
        public string[] Split(string[] separator, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, options);

        /// <summary>
        /// Splits the string by the specified separator, limiting the number of substrings.
        /// </summary>
        public string[] Split(char separator, int count, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, count, options);

        /// <summary>
        /// Splits the string by the specified separator, limiting the number of substrings.
        /// </summary>
        public string[] Split(string separator, int count, StringSplitOptions options = StringSplitOptions.None)
            => ToString().Split(separator, count, options);

        // -------------------------------------------------------------------------
        // PADDING
        // -------------------------------------------------------------------------

        /// <summary>
        /// Pads the string on the left with spaces to the specified total width.
        /// </summary>
        public string PadLeft(int totalWidth) => ToString().PadLeft(totalWidth);

        /// <summary>
        /// Pads the string on the left with the specified character to the specified total width.
        /// </summary>
        public string PadLeft(int totalWidth, char paddingChar) => ToString().PadLeft(totalWidth, paddingChar);

        /// <summary>
        /// Pads the string on the right with spaces to the specified total width.
        /// </summary>
        public string PadRight(int totalWidth) => ToString().PadRight(totalWidth);

        /// <summary>
        /// Pads the string on the right with the specified character to the specified total width.
        /// </summary>
        public string PadRight(int totalWidth, char paddingChar) => ToString().PadRight(totalWidth, paddingChar);

        // -------------------------------------------------------------------------
        // CONCATENATION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Concatenates this string with another string.
        /// </summary>
        public string Concat(string other) => string.Concat(ToString(), other);

        /// <summary>
        /// Concatenates this string with another GhostStringUtf8.
        /// </summary>
        public string Concat(GhostStringUtf8 other) => string.Concat(ToString(), other.ToString());

        /// <summary>
        /// Concatenates this string with a GhostStringUtf16.
        /// </summary>
        public string Concat(GhostStringUtf16 other) => string.Concat(ToString(), other.ToString());

        /// <summary>
        /// Concatenation operator for two GhostStringUtf8 values.
        /// </summary>
        public static string operator +(GhostStringUtf8 left, GhostStringUtf8 right) => string.Concat(left.ToString(), right.ToString());

        /// <summary>
        /// Concatenation operator for GhostStringUtf8 and string.
        /// </summary>
        public static string operator +(GhostStringUtf8 left, string right) => string.Concat(left.ToString(), right);

        /// <summary>
        /// Concatenation operator for string and GhostStringUtf8.
        /// </summary>
        public static string operator +(string left, GhostStringUtf8 right) => string.Concat(left, right.ToString());

        /// <summary>
        /// Concatenation operator for GhostStringUtf8 and GhostStringUtf16.
        /// </summary>
        public static string operator +(GhostStringUtf8 left, GhostStringUtf16 right) => string.Concat(left.ToString(), right.ToString());

        /// <summary>
        /// Concatenation operator for GhostStringUtf16 and GhostStringUtf8.
        /// </summary>
        public static string operator +(GhostStringUtf16 left, GhostStringUtf8 right) => string.Concat(left.ToString(), right.ToString());

        // -------------------------------------------------------------------------
        // INSERT / REMOVE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Inserts a string at the specified character index.
        /// </summary>
        public string Insert(int startIndex, string value) => ToString().Insert(startIndex, value);

        /// <summary>
        /// Removes all characters starting at the specified index.
        /// </summary>
        public string Remove(int startIndex) => ToString().Remove(startIndex);

        /// <summary>
        /// Removes a specified number of characters starting at the specified index.
        /// </summary>
        public string Remove(int startIndex, int count) => ToString().Remove(startIndex, count);

        // -------------------------------------------------------------------------
        // CHARACTER CHECKS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        public bool IsNullOrWhiteSpace() => string.IsNullOrWhiteSpace(ToString());

        /// <summary>
        /// Gets the first byte of the UTF-8 string.
        /// </summary>
        public byte FirstByte => ByteLength > 0 ? AsBytes()[0] : throw new InvalidOperationException("String is empty.");

        /// <summary>
        /// Gets the last byte of the UTF-8 string.
        /// </summary>
        public byte LastByte => ByteLength > 0 ? AsBytes()[ByteLength - 1] : throw new InvalidOperationException("String is empty.");

        /// <summary>
        /// Gets the first character of the string.
        /// </summary>
        public char First => Length > 0 ? ToString()[0] : throw new InvalidOperationException("String is empty.");

        /// <summary>
        /// Gets the last character of the string.
        /// </summary>
        public char Last {
            get {
                var str = ToString();
                return str.Length > 0 ? str[str.Length - 1] : throw new InvalidOperationException("String is empty.");
            }
        }

        /// <summary>
        /// Gets the first character of the string, or a default value if empty.
        /// </summary>
        public char FirstOrDefault(char defaultValue = default)
        {
            var str = ToString();
            return str.Length > 0 ? str[0] : defaultValue;
        }

        /// <summary>
        /// Gets the last character of the string, or a default value if empty.
        /// </summary>
        public char LastOrDefault(char defaultValue = default)
        {
            var str = ToString();
            return str.Length > 0 ? str[str.Length - 1] : defaultValue;
        }

        /// <summary>
        /// Gets the first byte of the UTF-8 string, or a default value if empty.
        /// </summary>
        public byte FirstByteOrDefault(byte defaultValue = default) => ByteLength > 0 ? AsBytes()[0] : defaultValue;

        /// <summary>
        /// Gets the last byte of the UTF-8 string, or a default value if empty.
        /// </summary>
        public byte LastByteOrDefault(byte defaultValue = default) => ByteLength > 0 ? AsBytes()[ByteLength - 1] : defaultValue;

        // -------------------------------------------------------------------------
        // HASHING
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the hash code for this string.
        /// </summary>
        public override int GetHashCode()
        {
            var bytes = AsBytes();
            if (bytes.IsEmpty) return 0;

            // Use a simple hash based on the UTF-8 bytes
            var hash = new HashCode();
            hash.AddBytes(bytes);
            return hash.ToHashCode();
        }

        /// <summary>
        /// Returns the hash code for this string using the specified comparison.
        /// </summary>
        public int GetHashCode(StringComparison comparisonType) => string.GetHashCode(ToString().AsSpan(), comparisonType);

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns an enumerator that iterates through the UTF-8 bytes of this string.
        /// </summary>
        public ReadOnlySpan<byte>.Enumerator GetEnumerator() => AsBytes().GetEnumerator();

        // -------------------------------------------------------------------------
        // CHAR ARRAY
        // -------------------------------------------------------------------------

        /// <summary>
        /// Copies the characters in this string to a character array.
        /// </summary>
        public char[] ToCharArray() => ToString().ToCharArray();

        /// <summary>
        /// Copies a specified substring to a character array.
        /// </summary>
        public char[] ToCharArray(int startIndex, int length) => ToString().ToCharArray(startIndex, length);

        // -------------------------------------------------------------------------
        // NORMALIZATION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a new string whose characters are a normalized form of this string.
        /// </summary>
        public string Normalize() => ToString().Normalize();

        /// <summary>
        /// Returns a new string whose characters are a normalized form of this string using the specified normalization form.
        /// </summary>
        public string Normalize(NormalizationForm normalizationForm) => ToString().Normalize(normalizationForm);

        /// <summary>
        /// Indicates whether this string is in the specified Unicode normalization form.
        /// </summary>
        public bool IsNormalized() => ToString().IsNormalized();

        /// <summary>
        /// Indicates whether this string is in the specified Unicode normalization form.
        /// </summary>
        public bool IsNormalized(NormalizationForm normalizationForm) => ToString().IsNormalized(normalizationForm);

        // -------------------------------------------------------------------------
        // FORMATTING
        // -------------------------------------------------------------------------

        /// <summary>
        /// Formats the string using the specified arguments.
        /// </summary>
        public string Format(params object?[] args) => string.Format(ToString(), args);

        /// <summary>
        /// Formats the string using the specified format provider and arguments.
        /// </summary>
        public string Format(IFormatProvider? provider, params object?[] args) => string.Format(provider, ToString(), args);

        // -------------------------------------------------------------------------
        // UTF-8 SPECIFIC OPERATIONS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Validates that the UTF-8 bytes are well-formed.
        /// </summary>
        public bool IsValidUtf8()
        {
            try
            {
                var bytes = AsBytes();
                if (bytes.IsEmpty) return true;
                _ = Encoding.UTF8.GetCharCount(bytes);
                return true;
            } catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the byte index for a given character index.
        /// Returns -1 if the character index is out of range.
        /// </summary>
        public int GetByteIndexForCharIndex(int charIndex)
        {
            if (charIndex < 0) return -1;

            var bytes = AsBytes();
            int byteIndex = 0;
            int currentCharIndex = 0;

            while (byteIndex < bytes.Length && currentCharIndex < charIndex)
            {
                var b = bytes[byteIndex];
                int sequenceLength = GetUtf8SequenceLength(b);
                if (sequenceLength == 0 || byteIndex + sequenceLength > bytes.Length)
                    return -1; // Invalid UTF-8

                byteIndex += sequenceLength;
                currentCharIndex++;
            }

            return currentCharIndex == charIndex ? byteIndex : -1;
        }

        /// <summary>
        /// Gets the character index for a given byte index.
        /// Returns -1 if the byte index is not at a character boundary.
        /// </summary>
        public int GetCharIndexForByteIndex(int byteIndex)
        {
            if (byteIndex < 0) return -1;

            var bytes = AsBytes();
            if (byteIndex > bytes.Length) return -1;
            if (byteIndex == 0) return 0;

            int currentByteIndex = 0;
            int charIndex = 0;

            while (currentByteIndex < byteIndex && currentByteIndex < bytes.Length)
            {
                var b = bytes[currentByteIndex];
                int sequenceLength = GetUtf8SequenceLength(b);
                if (sequenceLength == 0 || currentByteIndex + sequenceLength > bytes.Length)
                    return -1; // Invalid UTF-8

                currentByteIndex += sequenceLength;
                charIndex++;
            }

            return currentByteIndex == byteIndex ? charIndex : -1;
        }

        /// <summary>
        /// Gets the UTF-8 sequence length for a leading byte.
        /// </summary>
        private static int GetUtf8SequenceLength(byte leadingByte)
        {
            if ((leadingByte & 0x80) == 0) return 1;       // 0xxxxxxx
            if ((leadingByte & 0xE0) == 0xC0) return 2;    // 110xxxxx
            if ((leadingByte & 0xF0) == 0xE0) return 3;    // 1110xxxx
            if ((leadingByte & 0xF8) == 0xF0) return 4;    // 11110xxx
            return 0; // Invalid leading byte
        }

        // -------------------------------------------------------------------------
        // HELPER METHODS
        // -------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReadOnly() => throw new InvalidOperationException("This GhostStringUtf8 instance is read-only.");

        // -------------------------------------------------------------------------
        // OBJECT OVERRIDES (ref struct cannot inherit, but we provide these)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Not supported for ref struct. Use Equals(GhostStringUtf8) or Equals(string) instead.
        /// </summary>
        public override bool Equals(object? obj) => throw new NotSupportedException("Cannot box a ref struct. Use Equals(GhostStringUtf8) or Equals(string) instead.");
    }
}
