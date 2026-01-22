using System.Text;

namespace GhostBodyObject.Repository.Tests.Ghost.Values
{
    public class GhostStringUtf16Should
    {
        // -------------------------------------------------------------------------
        // CONSTRUCTOR AND BASIC PROPERTIES
        // -------------------------------------------------------------------------

        [Fact]
        public void CreateFromStringImplicitly()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("Hello, World!", ghost.ToString());
        }

        [Fact]
        public void CreateFromNullStringReturnsDefault()
        {
            GhostStringUtf16 ghost = (string?)null;
            Assert.True(ghost.IsEmpty);
            Assert.True(ghost.IsNullOrEmpty);
        }

        [Fact]
        public void CreateFromEmptyStringReturnsDefault()
        {
            GhostStringUtf16 ghost = "";
            Assert.True(ghost.IsEmpty);
            Assert.True(ghost.IsNullOrEmpty);
        }

        [Fact]
        public void ReturnCorrectLength()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal(5, ghost.Length);
        }

        [Fact]
        public void ReturnCorrectByteLength()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal(10, ghost.ByteLength); // 5 chars * 2 bytes per char
        }

        [Fact]
        public void ReturnIsEmptyForEmptyString()
        {
            GhostStringUtf16 ghost = default;
            Assert.True(ghost.IsEmpty);
        }

        [Fact]
        public void ReturnIsNotEmptyForNonEmptyString()
        {
            GhostStringUtf16 ghost = "Test";
            Assert.False(ghost.IsEmpty);
        }

        // -------------------------------------------------------------------------
        // INDEXER
        // -------------------------------------------------------------------------

        [Fact]
        public void AccessCharacterByIndex()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal('H', ghost[0]);
            Assert.Equal('e', ghost[1]);
            Assert.Equal('o', ghost[4]);
        }

        [Fact]
        public void ThrowOnInvalidIndexPositive()
        {
            // Cannot use ref struct in lambda, so test differently
            GhostStringUtf16 ghost = "Hello";
            try
            {
                _ = ghost[5];
                Assert.Fail("Expected IndexOutOfRangeException");
            } catch (IndexOutOfRangeException)
            {
                // Expected
            }
        }

        [Fact]
        public void ThrowOnInvalidIndexNegative()
        {
            GhostStringUtf16 ghost = "Hello";
            try
            {
                _ = ghost[-1];
                Assert.Fail("Expected IndexOutOfRangeException");
            } catch (IndexOutOfRangeException)
            {
                // Expected
            }
        }

        // -------------------------------------------------------------------------
        // SPAN ACCESS
        // -------------------------------------------------------------------------

        [Fact]
        public void ReturnCorrectSpan()
        {
            GhostStringUtf16 ghost = "Hello";
            var span = ghost.AsSpan();
            Assert.Equal(5, span.Length);
            Assert.Equal("Hello", new string(span));
        }

        [Fact]
        public void ReturnSlicedSpan()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            var span = ghost.AsSpan(7);
            Assert.Equal("World!", new string(span));
        }

        [Fact]
        public void ReturnSlicedSpanWithLength()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            var span = ghost.AsSpan(7, 5);
            Assert.Equal("World", new string(span));
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        [Fact]
        public void ConvertToStringImplicitly()
        {
            GhostStringUtf16 ghost = "Hello";
            string result = ghost;
            Assert.Equal("Hello", result);
        }

        [Fact]
        public void ConvertToUtf8Bytes()
        {
            GhostStringUtf16 ghost = "Hello";
            var utf8Bytes = ghost.ToUtf8Bytes();
            Assert.Equal(Encoding.UTF8.GetBytes("Hello"), utf8Bytes);
        }

        [Fact]
        public void CopyToDestinationSpan()
        {
            GhostStringUtf16 ghost = "Hello";
            var destination = new char[10];
            ghost.CopyTo(destination.AsSpan());
            Assert.Equal("Hello", new string(destination, 0, 5));
        }

        [Fact]
        public void TryCopyToDestinationSpan()
        {
            GhostStringUtf16 ghost = "Hello";
            var destination = new char[5];
            Assert.True(ghost.TryCopyTo(destination.AsSpan()));
            Assert.Equal("Hello", new string(destination));
        }

        [Fact]
        public void TryCopyToReturnsFalseWhenDestinationTooSmall()
        {
            GhostStringUtf16 ghost = "Hello";
            var destination = new char[3];
            Assert.False(ghost.TryCopyTo(destination.AsSpan()));
        }

        // -------------------------------------------------------------------------
        // COMPARISON - EQUALS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualSameString()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.True(ghost.Equals("Hello"));
        }

        [Fact]
        public void NotEqualDifferentString()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.False(ghost.Equals("World"));
        }

        [Fact]
        public void EqualAnotherGhostString()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = "Hello";
            Assert.True(ghost1.Equals(ghost2));
        }

        [Fact]
        public void EqualWithStringComparison()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.True(ghost.Equals("hello", StringComparison.OrdinalIgnoreCase));
            Assert.False(ghost.Equals("hello", StringComparison.Ordinal));
        }

        [Fact]
        public void EqualNullReturnsTrueForEmpty()
        {
            GhostStringUtf16 ghost = default;
            Assert.True(ghost.Equals((string?)null));
        }

        // -------------------------------------------------------------------------
        // COMPARISON - OPERATORS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualityOperatorWithGhostStrings()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = "Hello";
            Assert.True(ghost1 == ghost2);
        }

        [Fact]
        public void InequalityOperatorWithGhostStrings()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = "World";
            Assert.True(ghost1 != ghost2);
        }

        [Fact]
        public void EqualityOperatorWithString()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.True(ghost == "Hello");
            Assert.True("Hello" == ghost);
        }

        [Fact]
        public void ComparisonOperators()
        {
            GhostStringUtf16 a = "Apple";
            GhostStringUtf16 b = "Banana";
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
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal(0, ghost.CompareTo("Hello"));
            Assert.True(ghost.CompareTo("World") < 0);
            Assert.True(ghost.CompareTo("Apple") > 0);
        }

        [Fact]
        public void CompareToGhostString()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = "Hello";
            Assert.Equal(0, ghost1.CompareTo(ghost2));
        }

        [Fact]
        public void CompareToWithComparison()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal(0, ghost.CompareTo("hello", StringComparison.OrdinalIgnoreCase));
        }

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void IndexOfChar()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal(0, ghost.IndexOf('H'));
            Assert.Equal(7, ghost.IndexOf('W'));
            Assert.Equal(-1, ghost.IndexOf('Z'));
        }

        [Fact]
        public void IndexOfCharWithStartIndex()
        {
            GhostStringUtf16 ghost = "Hello, Hello!";
            Assert.Equal(7, ghost.IndexOf('H', 1));
        }

        [Fact]
        public void IndexOfCharWithRange()
        {
            GhostStringUtf16 ghost = "Hello, Hello, Hello!";
            Assert.Equal(7, ghost.IndexOf('H', 1, 10));
        }

        [Fact]
        public void IndexOfString()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal(7, ghost.IndexOf("World"));
            Assert.Equal(-1, ghost.IndexOf("Universe"));
        }

        [Fact]
        public void IndexOfStringWithComparison()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal(7, ghost.IndexOf("world", StringComparison.OrdinalIgnoreCase));
            Assert.Equal(-1, ghost.IndexOf("world", StringComparison.Ordinal));
        }

        [Fact]
        public void IndexOfAny()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal(4, ghost.IndexOfAny(['o', 'W']));
        }

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void LastIndexOfChar()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal(8, ghost.LastIndexOf('o'));
        }

        [Fact]
        public void LastIndexOfString()
        {
            GhostStringUtf16 ghost = "Hello, Hello!";
            Assert.Equal(7, ghost.LastIndexOf("Hello"));
        }

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        [Fact]
        public void ContainsChar()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.Contains('W'));
            Assert.False(ghost.Contains('Z'));
        }

        [Fact]
        public void ContainsString()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.Contains("World"));
            Assert.False(ghost.Contains("Universe"));
        }

        [Fact]
        public void ContainsStringWithComparison()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.Contains("world", StringComparison.OrdinalIgnoreCase));
        }

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        [Fact]
        public void StartsWithChar()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.True(ghost.StartsWith('H'));
            Assert.False(ghost.StartsWith('h'));
        }

        [Fact]
        public void StartsWithString()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.StartsWith("Hello"));
            Assert.False(ghost.StartsWith("World"));
        }

        [Fact]
        public void StartsWithStringIgnoreCase()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.StartsWith("hello", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void EndsWithChar()
        {
            GhostStringUtf16 ghost = "Hello!";
            Assert.True(ghost.EndsWith('!'));
            Assert.False(ghost.EndsWith('o'));
        }

        [Fact]
        public void EndsWithString()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.True(ghost.EndsWith("World!"));
            Assert.False(ghost.EndsWith("Hello"));
        }

        // -------------------------------------------------------------------------
        // SUBSTRING / SLICE
        // -------------------------------------------------------------------------

        [Fact]
        public void SubstringFromStart()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("World!", ghost.Substring(7));
        }

        [Fact]
        public void SubstringWithLength()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("World", ghost.Substring(7, 5));
        }

        [Fact]
        public void SliceFromStart()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            var sliced = ghost.Slice(7);
            Assert.Equal("World!", sliced.ToString());
        }

        [Fact]
        public void SliceWithLength()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            var sliced = ghost.Slice(7, 5);
            Assert.Equal("World", sliced.ToString());
        }

        // -------------------------------------------------------------------------
        // TRIM
        // -------------------------------------------------------------------------

        [Fact]
        public void TrimWhitespace()
        {
            GhostStringUtf16 ghost = "  Hello  ";
            Assert.Equal("Hello", ghost.Trim());
        }

        [Fact]
        public void TrimSpecificChar()
        {
            GhostStringUtf16 ghost = "***Hello***";
            Assert.Equal("Hello", ghost.Trim('*'));
        }

        [Fact]
        public void TrimStart()
        {
            GhostStringUtf16 ghost = "  Hello  ";
            Assert.Equal("Hello  ", ghost.TrimStart());
        }

        [Fact]
        public void TrimEnd()
        {
            GhostStringUtf16 ghost = "  Hello  ";
            Assert.Equal("  Hello", ghost.TrimEnd());
        }

        // -------------------------------------------------------------------------
        // CASE CONVERSION
        // -------------------------------------------------------------------------

        [Fact]
        public void ToUpperCase()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal("HELLO", ghost.ToUpper());
        }

        [Fact]
        public void ToLowerCase()
        {
            GhostStringUtf16 ghost = "HELLO";
            Assert.Equal("hello", ghost.ToLower());
        }

        [Fact]
        public void ToUpperInvariant()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal("HELLO", ghost.ToUpperInvariant());
        }

        [Fact]
        public void ToLowerInvariant()
        {
            GhostStringUtf16 ghost = "HELLO";
            Assert.Equal("hello", ghost.ToLowerInvariant());
        }

        // -------------------------------------------------------------------------
        // REPLACE
        // -------------------------------------------------------------------------

        [Fact]
        public void ReplaceChar()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal("Hallo", ghost.Replace('e', 'a'));
        }

        [Fact]
        public void ReplaceString()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("Hello, Universe!", ghost.Replace("World", "Universe"));
        }

        // -------------------------------------------------------------------------
        // SPLIT
        // -------------------------------------------------------------------------

        [Fact]
        public void SplitByChar()
        {
            GhostStringUtf16 ghost = "one,two,three";
            var parts = ghost.Split(',');
            Assert.Equal(3, parts.Length);
            Assert.Equal("one", parts[0]);
            Assert.Equal("two", parts[1]);
            Assert.Equal("three", parts[2]);
        }

        [Fact]
        public void SplitByString()
        {
            GhostStringUtf16 ghost = "one::two::three";
            var parts = ghost.Split("::");
            Assert.Equal(3, parts.Length);
        }

        // -------------------------------------------------------------------------
        // PADDING
        // -------------------------------------------------------------------------

        [Fact]
        public void PadLeft()
        {
            GhostStringUtf16 ghost = "42";
            Assert.Equal("  42", ghost.PadLeft(4));
            Assert.Equal("0042", ghost.PadLeft(4, '0'));
        }

        [Fact]
        public void PadRight()
        {
            GhostStringUtf16 ghost = "42";
            Assert.Equal("42  ", ghost.PadRight(4));
            Assert.Equal("4200", ghost.PadRight(4, '0'));
        }

        // -------------------------------------------------------------------------
        // CONCATENATION
        // -------------------------------------------------------------------------

        [Fact]
        public void ConcatWithString()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal("Hello, World!", ghost.Concat(", World!"));
        }

        [Fact]
        public void ConcatWithGhostString()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = ", World!";
            Assert.Equal("Hello, World!", ghost1.Concat(ghost2));
        }

        [Fact]
        public void ConcatOperator()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = ", World!";
            Assert.Equal("Hello, World!", ghost1 + ghost2);
            Assert.Equal("Hello, World!", ghost1 + ", World!");
            Assert.Equal("Hello, World!", "Hello" + ghost2);
        }

        // -------------------------------------------------------------------------
        // INSERT / REMOVE
        // -------------------------------------------------------------------------

        [Fact]
        public void InsertString()
        {
            GhostStringUtf16 ghost = "Hello!";
            Assert.Equal("Hello, World!", ghost.Insert(5, ", World"));
        }

        [Fact]
        public void RemoveFromIndex()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("Hello", ghost.Remove(5));
        }

        [Fact]
        public void RemoveWithCount()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            Assert.Equal("Hello World!", ghost.Remove(5, 1));
        }

        // -------------------------------------------------------------------------
        // CHARACTER CHECKS
        // -------------------------------------------------------------------------

        [Fact]
        public void IsNullOrWhitespace()
        {
            GhostStringUtf16 ghost1 = "   ";
            GhostStringUtf16 ghost2 = "Hello";
            Assert.True(ghost1.IsNullOrWhiteSpace());
            Assert.False(ghost2.IsNullOrWhiteSpace());
        }

        [Fact]
        public void GetFirstCharacter()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal('H', ghost.First);
        }

        [Fact]
        public void GetLastCharacter()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.Equal('o', ghost.Last);
        }

        [Fact]
        public void FirstOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf16 ghost = default;
            Assert.Equal('\0', ghost.FirstOrDefault());
            Assert.Equal('X', ghost.FirstOrDefault('X'));
        }

        [Fact]
        public void LastOrDefaultReturnsDefaultForEmpty()
        {
            GhostStringUtf16 ghost = default;
            Assert.Equal('\0', ghost.LastOrDefault());
            Assert.Equal('X', ghost.LastOrDefault('X'));
        }

        // -------------------------------------------------------------------------
        // HASHING
        // -------------------------------------------------------------------------

        [Fact]
        public void GetHashCodeForEqualStrings()
        {
            GhostStringUtf16 ghost1 = "Hello";
            GhostStringUtf16 ghost2 = "Hello";
            Assert.Equal(ghost1.GetHashCode(), ghost2.GetHashCode());
        }

        [Fact]
        public void GetHashCodeWithComparison()
        {
            GhostStringUtf16 ghost = "Hello";
            var hash = ghost.GetHashCode(StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual(0, hash);
        }

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        [Fact]
        public void EnumerateCharacters()
        {
            GhostStringUtf16 ghost = "Hi";
            var chars = new List<char>();
            foreach (var c in ghost)
            {
                chars.Add(c);
            }
            Assert.Equal(['H', 'i'], chars);
        }

        // -------------------------------------------------------------------------
        // CHAR ARRAY
        // -------------------------------------------------------------------------

        [Fact]
        public void ToCharArray()
        {
            GhostStringUtf16 ghost = "Hello";
            var chars = ghost.ToCharArray();
            Assert.Equal(['H', 'e', 'l', 'l', 'o'], chars);
        }

        [Fact]
        public void ToCharArrayWithRange()
        {
            GhostStringUtf16 ghost = "Hello, World!";
            var chars = ghost.ToCharArray(7, 5);
            Assert.Equal(['W', 'o', 'r', 'l', 'd'], chars);
        }

        // -------------------------------------------------------------------------
        // NORMALIZATION
        // -------------------------------------------------------------------------

        [Fact]
        public void Normalize()
        {
            GhostStringUtf16 ghost = "café";
            var normalized = ghost.Normalize();
            Assert.NotNull(normalized);
        }

        [Fact]
        public void IsNormalized()
        {
            GhostStringUtf16 ghost = "Hello";
            Assert.True(ghost.IsNormalized());
        }

        // -------------------------------------------------------------------------
        // FORMATTING
        // -------------------------------------------------------------------------

        [Fact]
        public void FormatWithArgs()
        {
            GhostStringUtf16 ghost = "Hello, {0}!";
            Assert.Equal("Hello, World!", ghost.Format("World"));
        }

        // -------------------------------------------------------------------------
        // UNICODE SUPPORT
        // -------------------------------------------------------------------------

        [Fact]
        public void HandleUnicodeCharacters()
        {
            GhostStringUtf16 ghost = "日本語テスト";
            Assert.Equal(6, ghost.Length);
            Assert.Equal("日本語テスト", ghost.ToString());
        }

        [Fact]
        public void HandleEmoji()
        {
            GhostStringUtf16 ghost = "Hello 👋 World";
            Assert.True(ghost.Contains("👋"));
        }
    }
}
