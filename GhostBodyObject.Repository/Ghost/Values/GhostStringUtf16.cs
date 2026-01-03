using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Experiments.BabyBody
{
    /// <summary>
    /// A ref struct that wraps UTF-16 string data stored in pinned memory, providing string-like semantics
    /// with efficient memory access. Designed to integrate seamlessly with the standard <see cref="string"/> class.
    /// </summary>
    public ref struct GhostStringUtf16
    {
        private readonly IEntityBody _body;
        private readonly PinnedMemory<byte> _data;
        private readonly int _arrayIndex;
        
        // When created from a string, we store the string directly and use its span
        // This avoids storing raw pointers to managed memory which could be moved by GC
        private readonly string? _sourceString;

        // -------------------------------------------------------------------------
        // CONSTRUCTORS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of GhostStringUtf16 with the specified body, array index, and pinned memory data.
        /// </summary>
        public GhostStringUtf16(IEntityBody body, int arrayIndex, PinnedMemory<byte> data)
        {
            _body = body;
            _arrayIndex = arrayIndex;
            _data = data;
            _sourceString = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostStringUtf16 with only pinned memory data (read-only access).
        /// </summary>
        public GhostStringUtf16(PinnedMemory<byte> data)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = data;
            _sourceString = null;
        }

        /// <summary>
        /// Initializes a new instance of GhostStringUtf16 from a string (read-only, used for implicit conversion).
        /// This is the safe way to wrap a string without storing raw pointers.
        /// </summary>
        private GhostStringUtf16(string source)
        {
            _body = null!;
            _arrayIndex = -1;
            _data = default;
            _sourceString = source;
        }

        // -------------------------------------------------------------------------
        // PROPERTIES
        // -------------------------------------------------------------------------

        /// <summary>
        /// Gets the length of the string in characters.
        /// </summary>
        public int Length => _sourceString != null ? _sourceString.Length : _data.Length / sizeof(char);

        /// <summary>
        /// Gets the length of the underlying data in bytes.
        /// </summary>
        public int ByteLength => _sourceString != null ? _sourceString.Length * sizeof(char) : _data.Length;

        /// <summary>
        /// Gets a value indicating whether this string is empty.
        /// </summary>
        public bool IsEmpty => Length == 0;

        /// <summary>
        /// Gets a value indicating whether this string is null or empty.
        /// </summary>
        public bool IsNullOrEmpty => _sourceString != null ? string.IsNullOrEmpty(_sourceString) : _data.IsEmpty;

        /// <summary>
        /// Gets the character at the specified index.
        /// </summary>
        public char this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Length)
                    ThrowIndexOutOfRange();
                return AsSpan()[index];
            }
        }

        // -------------------------------------------------------------------------
        // SPAN ACCESS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the string data as a ReadOnlySpan of characters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> AsSpan()
        {
            // If we have a source string, use its span directly (GC-safe)
            if (_sourceString != null)
                return _sourceString.AsSpan();
            
            return MemoryMarshal.Cast<byte, char>(_data.Span);
        }

        /// <summary>
        /// Returns the string data as a Span of characters (for modification).
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the source is a string (immutable).</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<char> AsWritableSpan()
        {
            if (_sourceString != null)
                ThrowReadOnly();
            
            return MemoryMarshal.Cast<byte, char>(_data.Span);
        }

        /// <summary>
        /// Returns a portion of the string as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> AsSpan(int start) => AsSpan().Slice(start);

        /// <summary>
        /// Returns a portion of the string as a ReadOnlySpan.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> AsSpan(int start, int length) => AsSpan().Slice(start, length);

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

            var union = Unsafe.As<BodyUnion>(_body);
            ((VectorTableHeader*)union._vTablePtr)->SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _arrayIndex);
        }

        /// <summary>
        /// Sets the string value from another GhostStringUtf16.
        /// </summary>
        public unsafe void SetString(GhostStringUtf16 value)
        {
            if (_body == null)
                ThrowReadOnly();

            var union = Unsafe.As<BodyUnion>(_body);
            ((VectorTableHeader*)union._vTablePtr)->SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _arrayIndex);
        }

        /// <summary>
        /// Sets the string value from a ReadOnlySpan of characters.
        /// </summary>
        public unsafe void SetString(ReadOnlySpan<char> value)
        {
            if (_body == null)
                ThrowReadOnly();

            var union = Unsafe.As<BodyUnion>(_body);
            ((VectorTableHeader*)union._vTablePtr)->SwapAnyArray(union, MemoryMarshal.AsBytes(value), _arrayIndex);
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        /// <summary>
        /// Converts this GhostStringUtf16 to a standard .NET string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            // If we have a source string, return it directly (no allocation)
            if (_sourceString != null)
                return _sourceString;
            
            return new string(AsSpan());
        }

        /// <summary>
        /// Implicit conversion to string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(GhostStringUtf16 value) => value.ToString();

        /// <summary>
        /// Implicit conversion from string to GhostStringUtf16.
        /// This creates a read-only GhostStringUtf16 that wraps the string safely without storing raw pointers.
        /// The string's span is accessed on-demand via AsSpan(), ensuring GC-safety.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GhostStringUtf16(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return default;

            return new GhostStringUtf16(value);
        }

        /// <summary>
        /// Implicit conversion to ReadOnlySpan&lt;char&gt;.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<char>(GhostStringUtf16 value) => value.AsSpan();

        /// <summary>
        /// Converts a string to UTF-8 byte array.
        /// </summary>
        public byte[] ToUtf8Bytes()
        {
            var span = AsSpan();
            var byteCount = Encoding.UTF8.GetByteCount(span);
            var bytes = new byte[byteCount];
            Encoding.UTF8.GetBytes(span, bytes);
            return bytes;
        }

        /// <summary>
        /// Copies the string to a destination span.
        /// </summary>
        public bool TryCopyTo(Span<char> destination)
        {
            return AsSpan().TryCopyTo(destination);
        }

        /// <summary>
        /// Copies the string to a destination span.
        /// </summary>
        public void CopyTo(Span<char> destination)
        {
            AsSpan().CopyTo(destination);
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
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(GhostStringUtf16 other)
        {
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        /// <summary>
        /// Determines whether this string equals the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<char> other)
        {
            return AsSpan().SequenceEqual(other);
        }

        /// <summary>
        /// Determines whether this string equals the specified string using the specified comparison.
        /// </summary>
        public bool Equals(string? other, StringComparison comparisonType)
        {
            if (other is null) return IsEmpty;
            return AsSpan().Equals(other.AsSpan(), comparisonType);
        }

        /// <summary>
        /// Determines whether this string equals the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool Equals(GhostStringUtf16 other, StringComparison comparisonType)
        {
            return AsSpan().Equals(other.AsSpan(), comparisonType);
        }

        /// <summary>
        /// Determines whether this string equals the specified span using the specified comparison.
        /// </summary>
        public bool Equals(ReadOnlySpan<char> other, StringComparison comparisonType)
        {
            return AsSpan().Equals(other, comparisonType);
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
            return AsSpan().CompareTo(other.AsSpan(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf16.
        /// </summary>
        public int CompareTo(GhostStringUtf16 other)
        {
            return AsSpan().CompareTo(other.AsSpan(), StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares this string to another string using the specified comparison.
        /// </summary>
        public int CompareTo(string? other, StringComparison comparisonType)
        {
            if (other is null) return IsEmpty ? 0 : 1;
            return AsSpan().CompareTo(other.AsSpan(), comparisonType);
        }

        /// <summary>
        /// Compares this string to another GhostStringUtf16 using the specified comparison.
        /// </summary>
        public int CompareTo(GhostStringUtf16 other, StringComparison comparisonType)
        {
            return AsSpan().CompareTo(other.AsSpan(), comparisonType);
        }

        // -------------------------------------------------------------------------
        // OPERATORS
        // -------------------------------------------------------------------------

        public static bool operator ==(GhostStringUtf16 left, GhostStringUtf16 right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf16 left, GhostStringUtf16 right) => !left.Equals(right);
        public static bool operator ==(GhostStringUtf16 left, string? right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf16 left, string? right) => !left.Equals(right);
        public static bool operator ==(string? left, GhostStringUtf16 right) => right.Equals(left);
        public static bool operator !=(string? left, GhostStringUtf16 right) => !right.Equals(left);
        public static bool operator ==(GhostStringUtf16 left, ReadOnlySpan<char> right) => left.Equals(right);
        public static bool operator !=(GhostStringUtf16 left, ReadOnlySpan<char> right) => !left.Equals(right);

        public static bool operator <(GhostStringUtf16 left, GhostStringUtf16 right) => left.CompareTo(right) < 0;
        public static bool operator >(GhostStringUtf16 left, GhostStringUtf16 right) => left.CompareTo(right) > 0;
        public static bool operator <=(GhostStringUtf16 left, GhostStringUtf16 right) => left.CompareTo(right) <= 0;
        public static bool operator >=(GhostStringUtf16 left, GhostStringUtf16 right) => left.CompareTo(right) >= 0;

        public static bool operator <(GhostStringUtf16 left, string? right) => left.CompareTo(right) < 0;
        public static bool operator >(GhostStringUtf16 left, string? right) => left.CompareTo(right) > 0;
        public static bool operator <=(GhostStringUtf16 left, string? right) => left.CompareTo(right) <= 0;
        public static bool operator >=(GhostStringUtf16 left, string? right) => left.CompareTo(right) >= 0;

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the first occurrence of the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(char value) => AsSpan().IndexOf(value);

        /// <summary>
        /// Returns the index of the first occurrence of the specified character, starting at the specified index.
        /// </summary>
        public int IndexOf(char value, int startIndex) => AsSpan(startIndex).IndexOf(value) is var idx && idx >= 0 ? idx + startIndex : -1;

        /// <summary>
        /// Returns the index of the first occurrence of the specified character within the specified range.
        /// </summary>
        public int IndexOf(char value, int startIndex, int count)
        {
            var idx = AsSpan(startIndex, count).IndexOf(value);
            return idx >= 0 ? idx + startIndex : -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(string value) => AsSpan().IndexOf(value.AsSpan());

        /// <summary>
        /// Returns the index of the first occurrence of the specified string using the specified comparison.
        /// </summary>
        public int IndexOf(string value, StringComparison comparisonType) => AsSpan().IndexOf(value.AsSpan(), comparisonType);

        /// <summary>
        /// Returns the index of the first occurrence of the specified string, starting at the specified index.
        /// </summary>
        public int IndexOf(string value, int startIndex)
        {
            var idx = AsSpan(startIndex).IndexOf(value.AsSpan());
            return idx >= 0 ? idx + startIndex : -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified string, starting at the specified index, using the specified comparison.
        /// </summary>
        public int IndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            var idx = AsSpan(startIndex).IndexOf(value.AsSpan(), comparisonType);
            return idx >= 0 ? idx + startIndex : -1;
        }

        /// <summary>
        /// Returns the index of the first occurrence of the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(GhostStringUtf16 value) => AsSpan().IndexOf(value.AsSpan());

        /// <summary>
        /// Returns the index of the first occurrence of the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(ReadOnlySpan<char> value) => AsSpan().IndexOf(value);

        /// <summary>
        /// Returns the index of the first occurrence of any of the specified characters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfAny(ReadOnlySpan<char> anyOf) => AsSpan().IndexOfAny(anyOf);

        /// <summary>
        /// Returns the index of the first occurrence of any of the specified characters.
        /// </summary>
        public int IndexOfAny(params char[] anyOf) => AsSpan().IndexOfAny(anyOf.AsSpan());

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the index of the last occurrence of the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(char value) => AsSpan().LastIndexOf(value);

        /// <summary>
        /// Returns the index of the last occurrence of the specified character, searching backward from the specified index.
        /// </summary>
        public int LastIndexOf(char value, int startIndex) => AsSpan(0, startIndex + 1).LastIndexOf(value);

        /// <summary>
        /// Returns the index of the last occurrence of the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(string value) => AsSpan().LastIndexOf(value.AsSpan());

        /// <summary>
        /// Returns the index of the last occurrence of the specified string using the specified comparison.
        /// </summary>
        public int LastIndexOf(string value, StringComparison comparisonType) => AsSpan().LastIndexOf(value.AsSpan(), comparisonType);

        /// <summary>
        /// Returns the index of the last occurrence of the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(GhostStringUtf16 value) => AsSpan().LastIndexOf(value.AsSpan());

        /// <summary>
        /// Returns the index of the last occurrence of the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOf(ReadOnlySpan<char> value) => AsSpan().LastIndexOf(value);

        /// <summary>
        /// Returns the index of the last occurrence of any of the specified characters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int LastIndexOfAny(ReadOnlySpan<char> anyOf) => AsSpan().LastIndexOfAny(anyOf);

        /// <summary>
        /// Returns the index of the last occurrence of any of the specified characters.
        /// </summary>
        public int LastIndexOfAny(params char[] anyOf) => AsSpan().LastIndexOfAny(anyOf.AsSpan());

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this string contains the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(char value) => AsSpan().Contains(value);

        /// <summary>
        /// Determines whether this string contains the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(string value) => AsSpan().Contains(value.AsSpan(), StringComparison.Ordinal);

        /// <summary>
        /// Determines whether this string contains the specified string using the specified comparison.
        /// </summary>
        public bool Contains(string value, StringComparison comparisonType) => AsSpan().Contains(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(GhostStringUtf16 value) => AsSpan().Contains(value.AsSpan(), StringComparison.Ordinal);

        /// <summary>
        /// Determines whether this string contains the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool Contains(GhostStringUtf16 value, StringComparison comparisonType) => AsSpan().Contains(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string contains the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(ReadOnlySpan<char> value) => AsSpan().Contains(value, StringComparison.Ordinal);

        /// <summary>
        /// Determines whether this string contains the specified span using the specified comparison.
        /// </summary>
        public bool Contains(ReadOnlySpan<char> value, StringComparison comparisonType) => AsSpan().Contains(value, comparisonType);

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        /// <summary>
        /// Determines whether this string starts with the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(char value) => Length > 0 && AsSpan()[0] == value;

        /// <summary>
        /// Determines whether this string starts with the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(string value) => AsSpan().StartsWith(value.AsSpan());

        /// <summary>
        /// Determines whether this string starts with the specified string using the specified comparison.
        /// </summary>
        public bool StartsWith(string value, StringComparison comparisonType) => AsSpan().StartsWith(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(GhostStringUtf16 value) => AsSpan().StartsWith(value.AsSpan());

        /// <summary>
        /// Determines whether this string starts with the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool StartsWith(GhostStringUtf16 value, StringComparison comparisonType) => AsSpan().StartsWith(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string starts with the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(ReadOnlySpan<char> value) => AsSpan().StartsWith(value);

        /// <summary>
        /// Determines whether this string starts with the specified span using the specified comparison.
        /// </summary>
        public bool StartsWith(ReadOnlySpan<char> value, StringComparison comparisonType) => AsSpan().StartsWith(value, comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified character.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(char value) => Length > 0 && AsSpan()[Length - 1] == value;

        /// <summary>
        /// Determines whether this string ends with the specified string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(string value) => AsSpan().EndsWith(value.AsSpan());

        /// <summary>
        /// Determines whether this string ends with the specified string using the specified comparison.
        /// </summary>
        public bool EndsWith(string value, StringComparison comparisonType) => AsSpan().EndsWith(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf16.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(GhostStringUtf16 value) => AsSpan().EndsWith(value.AsSpan());

        /// <summary>
        /// Determines whether this string ends with the specified GhostStringUtf16 using the specified comparison.
        /// </summary>
        public bool EndsWith(GhostStringUtf16 value, StringComparison comparisonType) => AsSpan().EndsWith(value.AsSpan(), comparisonType);

        /// <summary>
        /// Determines whether this string ends with the specified span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndsWith(ReadOnlySpan<char> value) => AsSpan().EndsWith(value);

        /// <summary>
        /// Determines whether this string ends with the specified span using the specified comparison.
        /// </summary>
        public bool EndsWith(ReadOnlySpan<char> value, StringComparison comparisonType) => AsSpan().EndsWith(value, comparisonType);

        // -------------------------------------------------------------------------
        // SUBSTRING / SLICE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns a substring starting at the specified index.
        /// </summary>
        public string Substring(int startIndex) => new string(AsSpan(startIndex));

        /// <summary>
        /// Returns a substring of the specified length starting at the specified index.
        /// </summary>
        public string Substring(int startIndex, int length) => new string(AsSpan(startIndex, length));

        /// <summary>
        /// Returns a slice of this string as a new GhostStringUtf16.
        /// Note: For string sources, this creates a substring (allocation).
        /// </summary>
        public GhostStringUtf16 Slice(int start)
        {
            if (_sourceString != null)
                return new GhostStringUtf16(_sourceString.Substring(start));
            
            return new GhostStringUtf16(_data.Slice(start * sizeof(char)));
        }

        /// <summary>
        /// Returns a slice of this string as a new GhostStringUtf16.
        /// Note: For string sources, this creates a substring (allocation).
        /// </summary>
        public GhostStringUtf16 Slice(int start, int length)
        {
            if (_sourceString != null)
                return new GhostStringUtf16(_sourceString.Substring(start, length));
            
            return new GhostStringUtf16(_data.Slice(start * sizeof(char), length * sizeof(char)));
        }

        // -------------------------------------------------------------------------
        // TRIM
        // -------------------------------------------------------------------------

        /// <summary>
        /// Removes all leading and trailing white-space characters and returns the result as a string.
        /// </summary>
        public string Trim() => AsSpan().Trim().ToString();

        /// <summary>
        /// Removes all leading and trailing occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string Trim(char trimChar) => AsSpan().Trim(trimChar).ToString();

        /// <summary>
        /// Removes all leading and trailing occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string Trim(ReadOnlySpan<char> trimChars) => AsSpan().Trim(trimChars).ToString();

        /// <summary>
        /// Removes all leading white-space characters and returns the result as a string.
        /// </summary>
        public string TrimStart() => AsSpan().TrimStart().ToString();

        /// <summary>
        /// Removes all leading occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string TrimStart(char trimChar) => AsSpan().TrimStart(trimChar).ToString();

        /// <summary>
        /// Removes all leading occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string TrimStart(ReadOnlySpan<char> trimChars) => AsSpan().TrimStart(trimChars).ToString();

        /// <summary>
        /// Removes all trailing white-space characters and returns the result as a string.
        /// </summary>
        public string TrimEnd() => AsSpan().TrimEnd().ToString();

        /// <summary>
        /// Removes all trailing occurrences of the specified character and returns the result as a string.
        /// </summary>
        public string TrimEnd(char trimChar) => AsSpan().TrimEnd(trimChar).ToString();

        /// <summary>
        /// Removes all trailing occurrences of the specified characters and returns the result as a string.
        /// </summary>
        public string TrimEnd(ReadOnlySpan<char> trimChars) => AsSpan().TrimEnd(trimChars).ToString();

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
        /// Concatenates this string with another GhostStringUtf16.
        /// </summary>
        public string Concat(GhostStringUtf16 other) => string.Concat(AsSpan(), other.AsSpan());

        /// <summary>
        /// Concatenates this string with a span.
        /// </summary>
        public string Concat(ReadOnlySpan<char> other) => string.Concat(AsSpan(), other);

        /// <summary>
        /// Concatenation operator for two GhostStringUtf16 values.
        /// </summary>
        public static string operator +(GhostStringUtf16 left, GhostStringUtf16 right) => string.Concat(left.AsSpan(), right.AsSpan());

        /// <summary>
        /// Concatenation operator for GhostStringUtf16 and string.
        /// </summary>
        public static string operator +(GhostStringUtf16 left, string right) => string.Concat(left.AsSpan(), right.AsSpan());

        /// <summary>
        /// Concatenation operator for string and GhostStringUtf16.
        /// </summary>
        public static string operator +(string left, GhostStringUtf16 right) => string.Concat(left.AsSpan(), right.AsSpan());

        // -------------------------------------------------------------------------
        // INSERT / REMOVE
        // -------------------------------------------------------------------------

        /// <summary>
        /// Inserts a string at the specified index.
        /// </summary>
        public string Insert(int startIndex, string value) => ToString().Insert(startIndex, value);

        /// <summary>
        /// Removes a specified number of characters starting at the specified index.
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
        public bool IsNullOrWhiteSpace() => AsSpan().IsWhiteSpace();

        /// <summary>
        /// Gets the first character of the string.
        /// </summary>
        public char First => Length > 0 ? AsSpan()[0] : throw new InvalidOperationException("String is empty.");

        /// <summary>
        /// Gets the last character of the string.
        /// </summary>
        public char Last => Length > 0 ? AsSpan()[Length - 1] : throw new InvalidOperationException("String is empty.");

        /// <summary>
        /// Gets the first character of the string, or a default value if empty.
        /// </summary>
        public char FirstOrDefault(char defaultValue = default) => Length > 0 ? AsSpan()[0] : defaultValue;

        /// <summary>
        /// Gets the last character of the string, or a default value if empty.
        /// </summary>
        public char LastOrDefault(char defaultValue = default) => Length > 0 ? AsSpan()[Length - 1] : defaultValue;

        // -------------------------------------------------------------------------
        // HASHING
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the hash code for this string.
        /// </summary>
        public override int GetHashCode() => string.GetHashCode(AsSpan());

        /// <summary>
        /// Returns the hash code for this string using the specified comparison.
        /// </summary>
        public int GetHashCode(StringComparison comparisonType) => string.GetHashCode(AsSpan(), comparisonType);

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns an enumerator that iterates through the characters of this string.
        /// </summary>
        public ReadOnlySpan<char>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

        // -------------------------------------------------------------------------
        // CHAR ARRAY
        // -------------------------------------------------------------------------

        /// <summary>
        /// Copies the characters in this string to a character array.
        /// </summary>
        public char[] ToCharArray() => AsSpan().ToArray();

        /// <summary>
        /// Copies a specified substring to a character array.
        /// </summary>
        public char[] ToCharArray(int startIndex, int length) => AsSpan(startIndex, length).ToArray();

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
        // HELPER METHODS
        // -------------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowReadOnly() => throw new InvalidOperationException("This GhostStringUtf16 instance is read-only.");

        // -------------------------------------------------------------------------
        // OBJECT OVERRIDES (ref struct cannot inherit, but we provide these)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Not supported for ref struct. Use Equals(GhostStringUtf16) or Equals(string) instead.
        /// </summary>
        public override bool Equals(object? obj) => throw new NotSupportedException("Cannot box a ref struct. Use Equals(GhostStringUtf16) or Equals(string) instead.");
    }
}
