using System;
using System.Text;

namespace GhostBodyObject.Repository.Tests.Ghost.Values
{
    public class GhostStringUtf8Should
    {
        // -------------------------------------------------------------------------
        // CONSTRUCTOR AND BASIC PROPERTIES
        // -------------------------------------------------------------------------

        [Fact]
        public void CreateFromStringImplicitly()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("Hello, World!", ghost.ToString());
        }

        [Fact]
        public void CreateFromNullStringReturnsDefault()
        {
            GhostStringUtf8 ghost = (string?)null;
            Assert.True(ghost.IsEmpty);
            Assert.True(ghost.IsNullOrEmpty);
        }

        [Fact]
        public void CreateFromEmptyStringReturnsDefault()
        {
            GhostStringUtf8 ghost = "";
            Assert.True(ghost.IsEmpty);
            Assert.True(ghost.IsNullOrEmpty);
        }

        [Fact]
        public void ReturnCorrectLength()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(5, ghost.Length);
        }

        [Fact]
        public void ReturnCorrectByteLength()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(5, ghost.ByteLength); // ASCII chars are 1 byte in UTF-8
        }

        [Fact]
        public void ReturnCorrectByteLengthForUnicode()
        {
            GhostStringUtf8 ghost = "日本語"; // 3 Japanese characters, 3 bytes each in UTF-8
            Assert.Equal(3, ghost.Length); // 3 characters
            Assert.Equal(9, ghost.ByteLength); // 9 bytes
        }

        [Fact]
        public void ReturnIsEmptyForEmptyString()
        {
            GhostStringUtf8 ghost = default;
            Assert.True(ghost.IsEmpty);
        }

        [Fact]
        public void ReturnIsNotEmptyForNonEmptyString()
        {
            GhostStringUtf8 ghost = "Test";
            Assert.False(ghost.IsEmpty);
        }

        // -------------------------------------------------------------------------
        // INDEXER (BYTE ACCESS)
        // -------------------------------------------------------------------------

        [Fact]
        public void AccessByteByIndex()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal((byte)'H', ghost[0]);
            Assert.Equal((byte)'e', ghost[1]);
            Assert.Equal((byte)'o', ghost[4]);
        }

        [Fact]
        public void ThrowOnInvalidIndexPositive()
        {
            var exceptionThrown = false;
            try
            {
                GhostStringUtf8 ghost = "Hello";
                _ = ghost[5];
            }
            catch (IndexOutOfRangeException)
            {
                exceptionThrown = true;
            }
            Assert.True(exceptionThrown, "Expected IndexOutOfRangeException");
        }

        [Fact]
        public void ThrowOnInvalidIndexNegative()
        {
            var exceptionThrown = false;
            try
            {
                GhostStringUtf8 ghost = "Hello";
                _ = ghost[-1];
            }
            catch (IndexOutOfRangeException)
            {
                exceptionThrown = true;
            }
            Assert.True(exceptionThrown, "Expected IndexOutOfRangeException");
        }

        // -------------------------------------------------------------------------
        // SPAN ACCESS
        // -------------------------------------------------------------------------

        [Fact]
        public void ReturnCorrectByteSpan()
        {
            GhostStringUtf8 ghost = "Hello";
            var bytes = ghost.AsBytes();
            Assert.Equal(5, bytes.Length);
            Assert.Equal(Encoding.UTF8.GetBytes("Hello"), bytes.ToArray());
        }

        [Fact]
        public void ReturnSlicedByteSpan()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var bytes = ghost.AsBytes(7);
            Assert.Equal("World!", Encoding.UTF8.GetString(bytes));
        }

        [Fact]
        public void ReturnSlicedByteSpanWithLength()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var bytes = ghost.AsBytes(7, 5);
            Assert.Equal("World", Encoding.UTF8.GetString(bytes));
        }

        [Fact]
        public void ReturnDecodedCharSpan()
        {
            GhostStringUtf8 ghost = "Hello";
            var span = ghost.AsSpan();
            Assert.Equal("Hello", new string(span));
        }

        [Fact]
        public void GetCharCount()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(5, ghost.GetCharCount());
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        [Fact]
        public void ConvertToStringImplicitly()
        {
            GhostStringUtf8 ghost = "Hello";
            string result = ghost;
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ConvertToUtf16()
        {
            GhostStringUtf8 ghost = "Hello";
            GhostStringUtf16 utf16 = ghost.ToUtf16();
            Assert.Equal("Hello", utf16.ToString());
        }

        [Fact]
        public void ConvertFromUtf16Explicitly()
        {
            GhostStringUtf16 utf16 = "Hello";
            GhostStringUtf8 utf8 = (GhostStringUtf8)utf16;
            Assert.Equal("Hello", utf8.ToString());
        }

        [Fact]
        public void ToArrayReturnsBytes()
        {
            GhostStringUtf8 ghost = "Hello";
            var bytes = ghost.ToArray();
            Assert.Equal(Encoding.UTF8.GetBytes("Hello"), bytes);
        }

        // -------------------------------------------------------------------------
        // COMPARISON - EQUALS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualSameString()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.True(ghost.Equals("Hello"));
        }

        [Fact]
        public void NotEqualDifferentString()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.False(ghost.Equals("World"));
        }

        [Fact]
        public void EqualAnotherGhostStringUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.True(ghost1.Equals(ghost2));
        }

        [Fact]
        public void EqualGhostStringUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello";
            GhostStringUtf16 ghost16 = "Hello";
            Assert.True(ghost8.Equals(ghost16));
        }

        [Fact]
        public void EqualWithStringComparison()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.True(ghost.Equals("hello", StringComparison.OrdinalIgnoreCase));
            Assert.False(ghost.Equals("hello", StringComparison.Ordinal));
        }

        [Fact]
        public void EqualNullReturnsTrueForEmpty()
        {
            GhostStringUtf8 ghost = default;
            Assert.True(ghost.Equals((string?)null));
        }

        // -------------------------------------------------------------------------
        // COMPARISON - OPERATORS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualityOperatorWithGhostStrings()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.True(ghost1 == ghost2);
        }

        [Fact]
        public void InequalityOperatorWithGhostStrings()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = "World";
            Assert.True(ghost1 != ghost2);
        }

        [Fact]
        public void EqualityOperatorWithString()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.True(ghost == "Hello");
            Assert.True("Hello" == ghost);
        }

        [Fact]
        public void EqualityOperatorWithUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello";
            GhostStringUtf16 ghost16 = "Hello";
            Assert.True(ghost8 == ghost16);
            Assert.True(ghost16 == ghost8);
        }

        [Fact]
        public void ComparisonOperators()
        {
            GhostStringUtf8 a = "Apple";
            GhostStringUtf8 b = "Banana";
            Assert.True(a < b);
            Assert.True(b > a);
            Assert.True(a <= b);
            Assert.True(b >= a);
            Assert.True(a <= "Apple");
            Assert.True(a >= "Apple");
        }

        // -------------------------------------------------------------------------
        // COMPARISON - COMPARE
        // -------------------------------------------------------------------------

        [Fact]
        public void CompareToString()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(0, ghost.CompareTo("Hello"));
            Assert.True(ghost.CompareTo("World") < 0);
            Assert.True(ghost.CompareTo("Apple") > 0);
        }

        [Fact]
        public void CompareToGhostStringUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.Equal(0, ghost1.CompareTo(ghost2));
        }

        [Fact]
        public void CompareToGhostStringUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello";
            GhostStringUtf16 ghost16 = "Hello";
            Assert.Equal(0, ghost8.CompareTo(ghost16));
        }

        [Fact]
        public void CompareToWithComparison()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(0, ghost.CompareTo("hello", StringComparison.OrdinalIgnoreCase));
        }

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void IndexOfChar()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal(0, ghost.IndexOf('H'));
            Assert.Equal(7, ghost.IndexOf('W'));
            Assert.Equal(-1, ghost.IndexOf('Z'));
        }

        [Fact]
        public void IndexOfCharWithStartIndex()
        {
            GhostStringUtf8 ghost = "Hello, Hello!";
            Assert.Equal(7, ghost.IndexOf('H', 1));
        }

        [Fact]
        public void IndexOfString()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal(7, ghost.IndexOf("World"));
            Assert.Equal(-1, ghost.IndexOf("Universe"));
        }

        [Fact]
        public void IndexOfStringWithComparison()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal(7, ghost.IndexOf("world", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(-1, ghost.IndexOf("world", StringComparison.Ordinal));
        }

        [Fact]
        public void IndexOfAny()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal(4, ghost.IndexOfAny(['o', 'W']));
        }

        // -------------------------------------------------------------------------
        // SEARCH - BYTE INDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void IndexOfByte()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(0, ghost.IndexOfByte((byte)'H'));
            Assert.Equal(1, ghost.IndexOfByte((byte)'e'));
            Assert.Equal(-1, ghost.IndexOfByte((byte)'Z'));
        }

        [Fact]
        public void IndexOfBytes()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var searchBytes = Encoding.UTF8.GetBytes("World");
            Assert.Equal(7, ghost.IndexOfBytes(searchBytes));
        }

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void LastIndexOfChar()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal(8, ghost.LastIndexOf('o'));
        }

        [Fact]
        public void LastIndexOfString()
        {
            GhostStringUtf8 ghost = "Hello, Hello!";
            Assert.Equal(7, ghost.LastIndexOf("Hello"));
        }

        [Fact]
        public void LastIndexOfByte()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(3, ghost.LastIndexOfByte((byte)'l'));
        }

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        [Fact]
        public void ContainsChar()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.Contains('W'));
            Assert.False(ghost.Contains('Z'));
        }

        [Fact]
        public void ContainsString()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.Contains("World"));
            Assert.False(ghost.Contains("Universe"));
        }

        [Fact]
        public void ContainsStringWithComparison()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.Contains("world", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ContainsBytes()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var searchBytes = Encoding.UTF8.GetBytes("World");
            Assert.True(ghost.ContainsBytes(searchBytes));
        }

        [Fact]
        public void ContainsUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello, World!";
            GhostStringUtf8 ghost2 = "World";
            Assert.True(ghost1.Contains(ghost2));
        }

        [Fact]
        public void ContainsUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello, World!";
            GhostStringUtf16 ghost16 = "World";
            Assert.True(ghost8.Contains(ghost16));
        }

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        [Fact]
        public void StartsWithChar()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.True(ghost.StartsWith('H'));
            Assert.False(ghost.StartsWith('h'));
        }

        [Fact]
        public void StartsWithString()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.StartsWith("Hello"));
            Assert.False(ghost.StartsWith("World"));
        }

        [Fact]
        public void StartsWithStringIgnoreCase()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.StartsWith("hello", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void StartsWithUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello, World!";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.True(ghost1.StartsWith(ghost2));
        }

        [Fact]
        public void StartsWithBytes()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var prefix = Encoding.UTF8.GetBytes("Hello");
            Assert.True(ghost.StartsWithBytes(prefix));
        }

        [Fact]
        public void EndsWithChar()
        {
            GhostStringUtf8 ghost = "Hello!";
            Assert.True(ghost.EndsWith('!'));
            Assert.False(ghost.EndsWith('o'));
        }

        [Fact]
        public void EndsWithString()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.True(ghost.EndsWith("World!"));
            Assert.False(ghost.EndsWith("Hello"));
        }

        [Fact]
        public void EndsWithUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello, World!";
            GhostStringUtf8 ghost2 = "World!";
            Assert.True(ghost1.EndsWith(ghost2));
        }

        [Fact]
        public void EndsWithBytes()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var suffix = Encoding.UTF8.GetBytes("World!");
            Assert.True(ghost.EndsWithBytes(suffix));
        }

        // -------------------------------------------------------------------------
        // SUBSTRING / SLICE
        // -------------------------------------------------------------------------

        [Fact]
        public void SubstringFromStart()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("World!", ghost.Substring(7));
        }

        [Fact]
        public void SubstringWithLength()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("World", ghost.Substring(7, 5));
        }

        [Fact]
        public void SliceBytesFromStart()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var sliced = ghost.SliceBytes(7);
            Assert.Equal("World!", sliced.ToString());
        }

        [Fact]
        public void SliceBytesWithLength()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var sliced = ghost.SliceBytes(7, 5);
            Assert.Equal("World", sliced.ToString());
        }

        // -------------------------------------------------------------------------
        // TRIM
        // -------------------------------------------------------------------------

        [Fact]
        public void TrimWhitespace()
        {
            GhostStringUtf8 ghost = "  Hello  ";
            Assert.Equal("Hello", ghost.Trim());
        }

        [Fact]
        public void TrimSpecificChar()
        {
            GhostStringUtf8 ghost = "***Hello***";
            Assert.Equal("Hello", ghost.Trim('*'));
        }

        [Fact]
        public void TrimStart()
        {
            GhostStringUtf8 ghost = "  Hello  ";
            Assert.Equal("Hello  ", ghost.TrimStart());
        }

        [Fact]
        public void TrimEnd()
        {
            GhostStringUtf8 ghost = "  Hello  ";
            Assert.Equal("  Hello", ghost.TrimEnd());
        }

        // -------------------------------------------------------------------------
        // CASE CONVERSION
        // -------------------------------------------------------------------------

        [Fact]
        public void ToUpperCase()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal("HELLO", ghost.ToUpper());
        }

        [Fact]
        public void ToLowerCase()
        {
            GhostStringUtf8 ghost = "HELLO";
            Assert.Equal("hello", ghost.ToLower());
        }

        [Fact]
        public void ToUpperInvariant()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal("HELLO", ghost.ToUpperInvariant());
        }

        [Fact]
        public void ToLowerInvariant()
        {
            GhostStringUtf8 ghost = "HELLO";
            Assert.Equal("hello", ghost.ToLowerInvariant());
        }

        // -------------------------------------------------------------------------
        // REPLACE
        // -------------------------------------------------------------------------

        [Fact]
        public void ReplaceChar()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal("Hallo", ghost.Replace('e', 'a'));
        }

        [Fact]
        public void ReplaceString()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("Hello, Universe!", ghost.Replace("World", "Universe"));
        }

        // -------------------------------------------------------------------------
        // SPLIT
        // -------------------------------------------------------------------------

        [Fact]
        public void SplitByChar()
        {
            GhostStringUtf8 ghost = "one,two,three";
            var parts = ghost.Split(',');
            Assert.Equal(3, parts.Length);
            Assert.Equal("one", parts[0]);
            Assert.Equal("two", parts[1]);
            Assert.Equal("three", parts[2]);
        }

        [Fact]
        public void SplitByString()
        {
            GhostStringUtf8 ghost = "one::two::three";
            var parts = ghost.Split("::");
            Assert.Equal(3, parts.Length);
        }

        // -------------------------------------------------------------------------
        // PADDING
        // -------------------------------------------------------------------------

        [Fact]
        public void PadLeft()
        {
            GhostStringUtf8 ghost = "42";
            Assert.Equal("  42", ghost.PadLeft(4));
            Assert.Equal("0042", ghost.PadLeft(4, '0'));
        }

        [Fact]
        public void PadRight()
        {
            GhostStringUtf8 ghost = "42";
            Assert.Equal("42  ", ghost.PadRight(4));
            Assert.Equal("4200", ghost.PadRight(4, '0'));
        }

        // -------------------------------------------------------------------------
        // CONCATENATION
        // -------------------------------------------------------------------------

        [Fact]
        public void ConcatWithString()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal("Hello, World!", ghost.Concat(", World!"));
        }

        [Fact]
        public void ConcatWithGhostStringUtf8()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = ", World!";
            Assert.Equal("Hello, World!", ghost1.Concat(ghost2));
        }

        [Fact]
        public void ConcatWithGhostStringUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello";
            GhostStringUtf16 ghost16 = ", World!";
            Assert.Equal("Hello, World!", ghost8.Concat(ghost16));
        }

        [Fact]
        public void ConcatOperator()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = ", World!";
            Assert.Equal("Hello, World!", ghost1 + ghost2);
            Assert.Equal("Hello, World!", ghost1 + ", World!");
            Assert.Equal("Hello, World!", "Hello" + ghost2);
        }

        [Fact]
        public void ConcatOperatorWithUtf16()
        {
            GhostStringUtf8 ghost8 = "Hello";
            GhostStringUtf16 ghost16 = ", World!";
            Assert.Equal("Hello, World!", ghost8 + ghost16);
            Assert.Equal(", World!Hello", ghost16 + ghost8);
        }

        // -------------------------------------------------------------------------
        // INSERT / REMOVE
        // -------------------------------------------------------------------------

        [Fact]
        public void InsertString()
        {
            GhostStringUtf8 ghost = "Hello!";
            Assert.Equal("Hello, World!", ghost.Insert(5, ", World"));
        }

        [Fact]
        public void RemoveFromIndex()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("Hello", ghost.Remove(5));
        }

        [Fact]
        public void RemoveWithCount()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            Assert.Equal("Hello World!", ghost.Remove(5, 1));
        }

        // -------------------------------------------------------------------------
        // CHARACTER CHECKS
        // -------------------------------------------------------------------------

        [Fact]
        public void IsNullOrWhitespace()
        {
            GhostStringUtf8 ghost1 = "   ";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.True(ghost1.IsNullOrWhiteSpace());
            Assert.False(ghost2.IsNullOrWhiteSpace());
        }

        [Fact]
        public void GetFirstByte()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal((byte)'H', ghost.FirstByte);
        }

        [Fact]
        public void GetLastByte()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal((byte)'o', ghost.LastByte);
        }

        [Fact]
        public void GetFirstCharacter()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal('H', ghost.First);
        }

        [Fact]
        public void GetLastCharacter()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal('o', ghost.Last);
        }

        [Fact]
        public void FirstOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf8 ghost = default;
            Assert.Equal('\0', ghost.FirstOrDefault());
            Assert.Equal('X', ghost.FirstOrDefault('X'));
        }

        [Fact]
        public void LastOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf8 ghost = default;
            Assert.Equal('\0', ghost.LastOrDefault());
            Assert.Equal('X', ghost.LastOrDefault('X'));
        }

        [Fact]
        public void FirstByteOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf8 ghost = default;
            Assert.Equal(0, ghost.FirstByteOrDefault());
            Assert.Equal(42, ghost.FirstByteOrDefault(42));
        }

        [Fact]
        public void LastByteOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf8 ghost = default;
            Assert.Equal(0, ghost.LastByteOrDefault());
            Assert.Equal(42, ghost.LastByteOrDefault(42));
        }

        // -------------------------------------------------------------------------
        // HASHING
        // -------------------------------------------------------------------------

        [Fact]
        public void GetHashCodeForEqualStrings()
        {
            GhostStringUtf8 ghost1 = "Hello";
            GhostStringUtf8 ghost2 = "Hello";
            Assert.Equal(ghost1.GetHashCode(), ghost2.GetHashCode());
        }

        [Fact]
        public void GetHashCodeWithComparison()
        {
            GhostStringUtf8 ghost = "Hello";
            var hash = ghost.GetHashCode(StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(0, hash);
        }

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        [Fact]
        public void EnumerateBytes()
        {
            GhostStringUtf8 ghost = "Hi";
            var bytes = new List<byte>();
            foreach (var b in ghost)
            {
                bytes.Add(b);
            }
            Assert.Equal([(byte)'H', (byte)'i'], bytes);
        }

        // -------------------------------------------------------------------------
        // CHAR ARRAY
        // -------------------------------------------------------------------------

        [Fact]
        public void ToCharArray()
        {
            GhostStringUtf8 ghost = "Hello";
            var chars = ghost.ToCharArray();
            Assert.Equal(['H', 'e', 'l', 'l', 'o'], chars);
        }

        [Fact]
        public void ToCharArrayWithRange()
        {
            GhostStringUtf8 ghost = "Hello, World!";
            var chars = ghost.ToCharArray(7, 5);
            Assert.Equal(['W', 'o', 'r', 'l', 'd'], chars);
        }

        // -------------------------------------------------------------------------
        // NORMALIZATION
        // -------------------------------------------------------------------------

        [Fact]
        public void Normalize()
        {
            GhostStringUtf8 ghost = "café";
            var normalized = ghost.Normalize();
            Assert.NotNull(normalized);
        }

        [Fact]
        public void IsNormalized()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.True(ghost.IsNormalized());
        }

        // -------------------------------------------------------------------------
        // FORMATTING
        // -------------------------------------------------------------------------

        [Fact]
        public void FormatWithArgs()
        {
            GhostStringUtf8 ghost = "Hello, {0}!";
            Assert.Equal("Hello, World!", ghost.Format("World"));
        }

        // -------------------------------------------------------------------------
        // UTF-8 SPECIFIC OPERATIONS
        // -------------------------------------------------------------------------

        [Fact]
        public void ValidateUtf8()
        {
            GhostStringUtf8 ghost = "Hello, 日本語!";
            Assert.True(ghost.IsValidUtf8());
        }

        [Fact]
        public void GetByteIndexForCharIndex()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(0, ghost.GetByteIndexForCharIndex(0));
            Assert.Equal(1, ghost.GetByteIndexForCharIndex(1));
            Assert.Equal(5, ghost.GetByteIndexForCharIndex(5));
        }

        [Fact]
        public void GetByteIndexForCharIndexWithMultibyteChars()
        {
            GhostStringUtf8 ghost = "日本語"; // 3 chars, 3 bytes each
            Assert.Equal(0, ghost.GetByteIndexForCharIndex(0));
            Assert.Equal(3, ghost.GetByteIndexForCharIndex(1));
            Assert.Equal(6, ghost.GetByteIndexForCharIndex(2));
            Assert.Equal(9, ghost.GetByteIndexForCharIndex(3));
        }

        [Fact]
        public void GetCharIndexForByteIndex()
        {
            GhostStringUtf8 ghost = "Hello";
            Assert.Equal(0, ghost.GetCharIndexForByteIndex(0));
            Assert.Equal(1, ghost.GetCharIndexForByteIndex(1));
            Assert.Equal(5, ghost.GetCharIndexForByteIndex(5));
        }

        [Fact]
        public void GetCharIndexForByteIndexWithMultibyteChars()
        {
            GhostStringUtf8 ghost = "日本語"; // 3 chars, 3 bytes each
            Assert.Equal(0, ghost.GetCharIndexForByteIndex(0));
            Assert.Equal(1, ghost.GetCharIndexForByteIndex(3));
            Assert.Equal(2, ghost.GetCharIndexForByteIndex(6));
            Assert.Equal(3, ghost.GetCharIndexForByteIndex(9));
        }

        [Fact]
        public void GetCharIndexForByteIndexReturnsMinusOneForNonBoundary()
        {
            GhostStringUtf8 ghost = "日本語"; // 3 chars, 3 bytes each
            Assert.Equal(-1, ghost.GetCharIndexForByteIndex(1)); // middle of first char
            Assert.Equal(-1, ghost.GetCharIndexForByteIndex(2)); // middle of first char
            Assert.Equal(-1, ghost.GetCharIndexForByteIndex(4)); // middle of second char
        }

        // -------------------------------------------------------------------------
        // UNICODE SUPPORT
        // -------------------------------------------------------------------------

        [Fact]
        public void HandleUnicodeCharacters()
        {
            GhostStringUtf8 ghost = "日本語テスト";
            Assert.Equal(6, ghost.Length);
            Assert.Equal(18, ghost.ByteLength); // 6 chars * 3 bytes each
            Assert.Equal("日本語テスト", ghost.ToString());
        }

        [Fact]
        public void HandleEmoji()
        {
            GhostStringUtf8 ghost = "Hello 👋 World";
            Assert.True(ghost.Contains("👋"));
        }

        [Fact]
        public void HandleMixedAsciiAndUnicode()
        {
            GhostStringUtf8 ghost = "Hello 世界!";
            Assert.Equal(9, ghost.Length); // "Hello " (6) + "世界" (2) + "!" (1)
            Assert.Equal(13, ghost.ByteLength); // 6 ASCII + 6 Unicode + 1 ASCII
            Assert.Equal("Hello 世界!", ghost.ToString());
        }

        // -------------------------------------------------------------------------
        // EDGE CASES
        // -------------------------------------------------------------------------

        [Fact]
        public void HandleEmptyStringOperations()
        {
            GhostStringUtf8 ghost = default;
            Assert.Equal("", ghost.ToString());
            Assert.Equal(0, ghost.Length);
            Assert.Equal(0, ghost.ByteLength);
            Assert.Equal(-1, ghost.IndexOf('a'));
            Assert.False(ghost.Contains("test"));
        }

        [Fact]
        public void HandleSingleCharacterString()
        {
            GhostStringUtf8 ghost = "X";
            Assert.Equal(1, ghost.Length);
            Assert.Equal(1, ghost.ByteLength);
            Assert.Equal('X', ghost.First);
            Assert.Equal('X', ghost.Last);
            Assert.Equal((byte)'X', ghost.FirstByte);
            Assert.Equal((byte)'X', ghost.LastByte);
        }
    }
}
