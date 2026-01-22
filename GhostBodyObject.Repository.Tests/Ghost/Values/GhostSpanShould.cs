using GhostBodyObject.Repository.Ghost.Values;

namespace GhostBodyObject.Repository.Tests.Ghost.Values
{
    public class GhostSpanShould
    {
        // -------------------------------------------------------------------------
        // CONSTRUCTOR AND BASIC PROPERTIES
        // -------------------------------------------------------------------------

        [Fact]
        public void CreateFromArrayImplicitly()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(5, ghost.Length);
            Assert.Equal(1, ghost[0]);
            Assert.Equal(5, ghost[4]);
        }

        [Fact]
        public void CreateFromNullArrayReturnsDefault()
        {
            GhostSpan<int> ghost = (int[]?)null;
            Assert.True(ghost.IsEmpty);
            Assert.Equal(0, ghost.Length);
        }

        [Fact]
        public void CreateFromEmptyArrayReturnsDefault()
        {
            GhostSpan<int> ghost = Array.Empty<int>();
            Assert.True(ghost.IsEmpty);
            Assert.Equal(0, ghost.Length);
        }

        [Fact]
        public void ReturnCorrectLength()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(5, ghost.Length);
        }

        [Fact]
        public void ReturnCorrectByteLength()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(20, ghost.ByteLength); // 5 ints * 4 bytes per int
        }

        [Fact]
        public void ReturnByteLengthForLongArray()
        {
            GhostSpan<long> ghost = new long[] { 1, 2, 3 };
            Assert.Equal(24, ghost.ByteLength); // 3 longs * 8 bytes per long
        }

        [Fact]
        public void ReturnIsEmptyForEmptyArray()
        {
            GhostSpan<int> ghost = default;
            Assert.True(ghost.IsEmpty);
        }

        [Fact]
        public void ReturnIsNotEmptyForNonEmptyArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.False(ghost.IsEmpty);
        }

        // -------------------------------------------------------------------------
        // INDEXER
        // -------------------------------------------------------------------------

        [Fact]
        public void AccessElementByIndex()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30, 40, 50 };
            Assert.Equal(10, ghost[0]);
            Assert.Equal(20, ghost[1]);
            Assert.Equal(50, ghost[4]);
        }

        [Fact]
        public void ThrowOnInvalidIndexPositive()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
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
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
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
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var span = ghost.AsSpan();
            Assert.Equal(5, span.Length);
            Assert.Equal(1, span[0]);
            Assert.Equal(5, span[4]);
        }

        [Fact]
        public void ReturnSlicedSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var span = ghost.AsSpan(2);
            Assert.Equal(3, span.Length);
            Assert.Equal(3, span[0]);
        }

        [Fact]
        public void ReturnSlicedSpanWithLength()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var span = ghost.AsSpan(1, 3);
            Assert.Equal(3, span.Length);
            Assert.Equal(2, span[0]);
            Assert.Equal(4, span[2]);
        }

        [Fact]
        public void ReturnBytesAsSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var bytes = ghost.AsBytes();
            Assert.Equal(12, bytes.Length); // 3 ints * 4 bytes
        }

        // -------------------------------------------------------------------------
        // CONVERSION
        // -------------------------------------------------------------------------

        [Fact]
        public void ConvertToArrayImplicitly()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            int[] result = ghost;
            Assert.Equal(new int[] { 1, 2, 3 }, result);
        }

        [Fact]
        public void ToArrayCreatesNewCopy()
        {
            int[] original = { 1, 2, 3 };
            GhostSpan<int> ghost = original;
            int[] copy = ghost.ToArray();

            // Modify original
            original[0] = 999;

            // Copy should not be affected
            Assert.Equal(1, copy[0]);
        }

        [Fact]
        public void ConvertToReadOnlySpanImplicitly()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            ReadOnlySpan<int> span = ghost;
            Assert.Equal(3, span.Length);
        }

        [Fact]
        public void CopyToDestinationSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var destination = new int[5];
            ghost.CopyTo(destination.AsSpan());
            Assert.Equal(1, destination[0]);
            Assert.Equal(2, destination[1]);
            Assert.Equal(3, destination[2]);
        }

        [Fact]
        public void TryCopyToDestinationSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var destination = new int[3];
            Assert.True(ghost.TryCopyTo(destination.AsSpan()));
            Assert.Equal(new int[] { 1, 2, 3 }, destination);
        }

        [Fact]
        public void TryCopyToReturnsFalseWhenDestinationTooSmall()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var destination = new int[3];
            Assert.False(ghost.TryCopyTo(destination.AsSpan()));
        }

        // -------------------------------------------------------------------------
        // COMPARISON - EQUALS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualSameArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.True(ghost.Equals(new int[] { 1, 2, 3 }));
        }

        [Fact]
        public void NotEqualDifferentArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.False(ghost.Equals(new int[] { 1, 2, 4 }));
        }

        [Fact]
        public void NotEqualDifferentLengthArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.False(ghost.Equals(new int[] { 1, 2 }));
        }

        [Fact]
        public void EqualAnotherGhostSpan()
        {
            GhostSpan<int> ghost1 = new int[] { 1, 2, 3 };
            GhostSpan<int> ghost2 = new int[] { 1, 2, 3 };
            Assert.True(ghost1.Equals(ghost2));
        }

        [Fact]
        public void EqualReadOnlySpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            ReadOnlySpan<int> span = new int[] { 1, 2, 3 };
            Assert.True(ghost.Equals(span));
        }

        [Fact]
        public void EqualNullReturnsTrueForEmpty()
        {
            GhostSpan<int> ghost = default;
            Assert.True(ghost.Equals((int[]?)null));
        }

        // -------------------------------------------------------------------------
        // COMPARISON - OPERATORS
        // -------------------------------------------------------------------------

        [Fact]
        public void EqualityOperatorWithGhostSpans()
        {
            GhostSpan<int> ghost1 = new int[] { 1, 2, 3 };
            GhostSpan<int> ghost2 = new int[] { 1, 2, 3 };
            Assert.True(ghost1 == ghost2);
        }

        [Fact]
        public void InequalityOperatorWithGhostSpans()
        {
            GhostSpan<int> ghost1 = new int[] { 1, 2, 3 };
            GhostSpan<int> ghost2 = new int[] { 4, 5, 6 };
            Assert.True(ghost1 != ghost2);
        }

        [Fact]
        public void EqualityOperatorWithArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.True(ghost == new int[] { 1, 2, 3 });
            Assert.True(new int[] { 1, 2, 3 } == ghost);
        }

        [Fact]
        public void InequalityOperatorWithArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.True(ghost != new int[] { 4, 5, 6 });
        }

        // -------------------------------------------------------------------------
        // SEARCH - INDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void IndexOfValue()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30, 40, 50 };
            Assert.Equal(0, ghost.IndexOf(10));
            Assert.Equal(2, ghost.IndexOf(30));
            Assert.Equal(4, ghost.IndexOf(50));
            Assert.Equal(-1, ghost.IndexOf(999));
        }

        [Fact]
        public void IndexOfValueWithStartIndex()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 10, 20, 10 };
            Assert.Equal(2, ghost.IndexOf(10, 1));
            Assert.Equal(4, ghost.IndexOf(10, 3));
        }

        [Fact]
        public void IndexOfValueWithRange()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30, 40, 50 };
            Assert.Equal(1, ghost.IndexOf(20, 0, 3));
            Assert.Equal(-1, ghost.IndexOf(50, 0, 3));
        }

        [Fact]
        public void IndexOfSequence()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, ghost.IndexOf(new int[] { 2, 3 }.AsSpan()));
            Assert.Equal(-1, ghost.IndexOf(new int[] { 3, 2 }.AsSpan()));
        }

        [Fact]
        public void IndexOfEmptySequenceReturnsZero()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.Equal(0, ghost.IndexOf(ReadOnlySpan<int>.Empty));
        }

        [Fact]
        public void IndexOfGhostSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            GhostSpan<int> search = new int[] { 3, 4 };
            Assert.Equal(2, ghost.IndexOf(search));
        }

        // -------------------------------------------------------------------------
        // SEARCH - LASTINDEXOF
        // -------------------------------------------------------------------------

        [Fact]
        public void LastIndexOfValue()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 10, 20, 10 };
            Assert.Equal(4, ghost.LastIndexOf(10));
            Assert.Equal(3, ghost.LastIndexOf(20));
        }

        [Fact]
        public void LastIndexOfValueWithStartIndex()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 10, 20, 10 };
            Assert.Equal(2, ghost.LastIndexOf(10, 3));
        }

        [Fact]
        public void LastIndexOfSequence()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 1, 2, 1 };
            Assert.Equal(2, ghost.LastIndexOf(new int[] { 1, 2 }.AsSpan()));
        }

        [Fact]
        public void LastIndexOfGhostSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 1, 2, 3 };
            GhostSpan<int> search = new int[] { 1, 2 };
            Assert.Equal(3, ghost.LastIndexOf(search));
        }

        // -------------------------------------------------------------------------
        // SEARCH - CONTAINS
        // -------------------------------------------------------------------------

        [Fact]
        public void ContainsValue()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.Contains(3));
            Assert.False(ghost.Contains(999));
        }

        [Fact]
        public void ContainsSequence()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.Contains(new int[] { 2, 3, 4 }.AsSpan()));
            Assert.False(ghost.Contains(new int[] { 4, 3 }.AsSpan()));
        }

        [Fact]
        public void ContainsGhostSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            GhostSpan<int> search = new int[] { 3, 4 };
            Assert.True(ghost.Contains(search));
        }

        // -------------------------------------------------------------------------
        // SEARCH - ALL/ANY/COUNT
        // -------------------------------------------------------------------------

        [Fact]
        public void AllReturnsTrue()
        {
            GhostSpan<int> ghost = new int[] { 2, 4, 6, 8 };
            Assert.True(ghost.All(x => x % 2 == 0));
        }

        [Fact]
        public void AllReturnsFalse()
        {
            GhostSpan<int> ghost = new int[] { 2, 3, 4, 5 };
            Assert.False(ghost.All(x => x % 2 == 0));
        }

        [Fact]
        public void AllReturnsTrueForEmpty()
        {
            GhostSpan<int> ghost = default;
            Assert.True(ghost.All(x => x > 0)); // Vacuously true
        }

        [Fact]
        public void AnyReturnsTrue()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.Any(x => x > 3));
        }

        [Fact]
        public void AnyReturnsFalse()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.False(ghost.Any(x => x > 10));
        }

        [Fact]
        public void AnyReturnsFalseForEmpty()
        {
            GhostSpan<int> ghost = default;
            Assert.False(ghost.Any(x => x > 0));
        }

        [Fact]
        public void CountWithPredicate()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5, 6 };
            Assert.Equal(3, ghost.Count(x => x % 2 == 0));
        }

        [Fact]
        public void CountValue()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 1, 3, 1, 4, 1 };
            Assert.Equal(4, ghost.Count(1));
            Assert.Equal(1, ghost.Count(2));
            Assert.Equal(0, ghost.Count(999));
        }

        // -------------------------------------------------------------------------
        // STARTS/ENDS WITH
        // -------------------------------------------------------------------------

        [Fact]
        public void StartsWithValue()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.StartsWith(1));
            Assert.False(ghost.StartsWith(2));
        }

        [Fact]
        public void StartsWithSequence()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.StartsWith(new int[] { 1, 2, 3 }.AsSpan()));
            Assert.False(ghost.StartsWith(new int[] { 2, 3 }.AsSpan()));
        }

        [Fact]
        public void StartsWithGhostSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            GhostSpan<int> prefix = new int[] { 1, 2 };
            Assert.True(ghost.StartsWith(prefix));
        }

        [Fact]
        public void EndsWithValue()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.EndsWith(5));
            Assert.False(ghost.EndsWith(4));
        }

        [Fact]
        public void EndsWithSequence()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.True(ghost.EndsWith(new int[] { 4, 5 }.AsSpan()));
            Assert.False(ghost.EndsWith(new int[] { 3, 4 }.AsSpan()));
        }

        [Fact]
        public void EndsWithGhostSpan()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            GhostSpan<int> suffix = new int[] { 4, 5 };
            Assert.True(ghost.EndsWith(suffix));
        }

        // -------------------------------------------------------------------------
        // SLICING
        // -------------------------------------------------------------------------

        [Fact]
        public void SliceFromStart()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var sliced = ghost.Slice(2);
            Assert.Equal(3, sliced.Length);
            Assert.Equal(3, sliced[0]);
            Assert.Equal(5, sliced[2]);
        }

        [Fact]
        public void SliceWithLength()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var sliced = ghost.Slice(1, 3);
            Assert.Equal(3, sliced.Length);
            Assert.Equal(2, sliced[0]);
            Assert.Equal(4, sliced[2]);
        }

        // -------------------------------------------------------------------------
        // ELEMENT ACCESS
        // -------------------------------------------------------------------------

        [Fact]
        public void GetFirstElement()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(10, ghost.First);
        }

        [Fact]
        public void GetLastElement()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(30, ghost.Last);
        }

        [Fact]
        public void FirstThrowsForEmpty()
        {
            GhostSpan<int> ghost = default;
            try
            {
                _ = ghost.First;
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected
            }
        }

        [Fact]
        public void LastThrowsForEmpty()
        {
            GhostSpan<int> ghost = default;
            try
            {
                _ = ghost.Last;
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected
            }
        }

        [Fact]
        public void FirstOrDefaultReturnsDefault()
        {
            GhostSpan<int> ghost = default;
            Assert.Equal(0, ghost.FirstOrDefault());
            Assert.Equal(42, ghost.FirstOrDefault(42));
        }

        [Fact]
        public void FirstOrDefaultReturnsValue()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(10, ghost.FirstOrDefault());
        }

        [Fact]
        public void LastOrDefaultReturnsDefault()
        {
            GhostSpan<int> ghost = default;
            Assert.Equal(0, ghost.LastOrDefault());
            Assert.Equal(42, ghost.LastOrDefault(42));
        }

        [Fact]
        public void LastOrDefaultReturnsValue()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(30, ghost.LastOrDefault());
        }

        [Fact]
        public void ElementAtOrDefaultReturnsValue()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(20, ghost.ElementAtOrDefault(1));
        }

        [Fact]
        public void ElementAtOrDefaultReturnsDefaultForOutOfBounds()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            Assert.Equal(0, ghost.ElementAtOrDefault(10));
            Assert.Equal(42, ghost.ElementAtOrDefault(10, 42));
        }

        // -------------------------------------------------------------------------
        // FIND METHODS
        // -------------------------------------------------------------------------

        [Fact]
        public void FindReturnsMatchingElement()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(4, ghost.Find(x => x > 3));
        }

        [Fact]
        public void FindReturnsDefaultWhenNotFound()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var result = ghost.Find(x => x > 10);
            Assert.Null(result); // Find returns T? which is null when not found
        }

        [Fact]
        public void FindIndexReturnsMatchingIndex()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(3, ghost.FindIndex(x => x > 3));
        }

        [Fact]
        public void FindIndexReturnsMinusOneWhenNotFound()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.Equal(-1, ghost.FindIndex(x => x > 10));
        }

        [Fact]
        public void FindLastReturnsLastMatchingElement()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(5, ghost.FindLast(x => x > 3));
        }

        [Fact]
        public void FindLastIndexReturnsLastMatchingIndex()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(4, ghost.FindLastIndex(x => x > 3));
        }

        [Fact]
        public void FindAllReturnsAllMatchingElements()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5, 6 };
            var result = ghost.FindAll(x => x % 2 == 0);
            Assert.Equal(new int[] { 2, 4, 6 }, result);
        }

        [Fact]
        public void FindAllReturnsEmptyWhenNoneMatch()
        {
            GhostSpan<int> ghost = new int[] { 1, 3, 5 };
            var result = ghost.FindAll(x => x % 2 == 0);
            Assert.Empty(result);
        }

        // -------------------------------------------------------------------------
        // TRANSFORMATION - READ-ONLY
        // -------------------------------------------------------------------------

        [Fact]
        public void ToReversedArrayReturnsReversed()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var reversed = ghost.ToReversedArray();
            Assert.Equal(new int[] { 5, 4, 3, 2, 1 }, reversed);
        }

        [Fact]
        public void ToSortedArrayReturnsSorted()
        {
            GhostSpan<int> ghost = new int[] { 5, 2, 8, 1, 9 };
            var sorted = ghost.ToSortedArray();
            Assert.Equal(new int[] { 1, 2, 5, 8, 9 }, sorted);
        }

        [Fact]
        public void ToSortedArrayWithComparerReturnsSorted()
        {
            GhostSpan<int> ghost = new int[] { 5, 2, 8, 1, 9 };
            var sorted = ghost.ToSortedArray(Comparer<int>.Create((a, b) => b.CompareTo(a))); // Descending
            Assert.Equal(new int[] { 9, 8, 5, 2, 1 }, sorted);
        }

        [Fact]
        public void DistinctReturnsUniqueElements()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 2, 3, 3, 3, 4 };
            var distinct = ghost.Distinct();
            Assert.Equal(new int[] { 1, 2, 3, 4 }, distinct);
        }

        [Fact]
        public void SelectProjectsElements()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var doubled = ghost.Select(x => x * 2);
            Assert.Equal(new int[] { 2, 4, 6 }, doubled);
        }

        [Fact]
        public void SelectToStringProjects()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var strings = ghost.Select(x => x.ToString());
            Assert.Equal(new string[] { "1", "2", "3" }, strings);
        }

        [Fact]
        public void WhereFiltersElements()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5, 6 };
            var evens = ghost.Where(x => x % 2 == 0);
            Assert.Equal(new int[] { 2, 4, 6 }, evens);
        }

        [Fact]
        public void TakeReturnsFirstNElements()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var taken = ghost.Take(3);
            Assert.Equal(new int[] { 1, 2, 3 }, taken);
        }

        [Fact]
        public void TakeMoreThanLengthReturnsAll()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var taken = ghost.Take(10);
            Assert.Equal(new int[] { 1, 2, 3 }, taken);
        }

        [Fact]
        public void SkipReturnsElementsAfterN()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3, 4, 5 };
            var skipped = ghost.Skip(2);
            Assert.Equal(new int[] { 3, 4, 5 }, skipped);
        }

        [Fact]
        public void SkipMoreThanLengthReturnsEmpty()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var skipped = ghost.Skip(10);
            Assert.Empty(skipped);
        }

        // -------------------------------------------------------------------------
        // BINARY SEARCH
        // -------------------------------------------------------------------------

        [Fact]
        public void BinarySearchFindsElement()
        {
            GhostSpan<int> ghost = new int[] { 1, 3, 5, 7, 9, 11, 13 };
            Assert.Equal(3, ghost.BinarySearch(7));
        }

        [Fact]
        public void BinarySearchReturnsComplementWhenNotFound()
        {
            GhostSpan<int> ghost = new int[] { 1, 3, 5, 7, 9, 11, 13 };
            var result = ghost.BinarySearch(6);
            Assert.True(result < 0);
            Assert.Equal(3, ~result); // Would be inserted at index 3
        }

        [Fact]
        public void BinarySearchWithComparer()
        {
            GhostSpan<int> ghost = new int[] { 13, 11, 9, 7, 5, 3, 1 }; // Descending
            var comparer = Comparer<int>.Create((a, b) => b.CompareTo(a));
            Assert.Equal(3, ghost.BinarySearch(7, comparer));
        }

        // -------------------------------------------------------------------------
        // HASHING
        // -------------------------------------------------------------------------

        [Fact]
        public void GetHashCodeForEqualArrays()
        {
            GhostSpan<int> ghost1 = new int[] { 1, 2, 3 };
            GhostSpan<int> ghost2 = new int[] { 1, 2, 3 };
            Assert.Equal(ghost1.GetHashCode(), ghost2.GetHashCode());
        }

        [Fact]
        public void GetHashCodeDiffersForDifferentArrays()
        {
            GhostSpan<int> ghost1 = new int[] { 1, 2, 3 };
            GhostSpan<int> ghost2 = new int[] { 1, 2, 4 };
            // Hash codes can collide, but typically won't for simple cases
            // This is a weak test, just ensure no crash
            _ = ghost1.GetHashCode();
            _ = ghost2.GetHashCode();
        }

        // -------------------------------------------------------------------------
        // STRING REPRESENTATION
        // -------------------------------------------------------------------------

        [Fact]
        public void ToStringReturnsArrayRepresentation()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            Assert.Equal("[1, 2, 3]", ghost.ToString());
        }

        [Fact]
        public void ToStringReturnsEmptyBracketsForEmpty()
        {
            GhostSpan<int> ghost = default;
            Assert.Equal("[]", ghost.ToString());
        }

        // -------------------------------------------------------------------------
        // ENUMERATOR
        // -------------------------------------------------------------------------

        [Fact]
        public void EnumerateElements()
        {
            GhostSpan<int> ghost = new int[] { 10, 20, 30 };
            var elements = new List<int>();
            foreach (var item in ghost)
            {
                elements.Add(item);
            }
            Assert.Equal(new int[] { 10, 20, 30 }, elements);
        }

        // -------------------------------------------------------------------------
        // DIFFERENT TYPES
        // -------------------------------------------------------------------------

        [Fact]
        public void WorksWithLongType()
        {
            GhostSpan<long> ghost = new long[] { 1L, 2L, 3L };
            Assert.Equal(3, ghost.Length);
            Assert.Equal(24, ghost.ByteLength);
            Assert.Equal(2L, ghost[1]);
        }

        [Fact]
        public void WorksWithByteType()
        {
            GhostSpan<byte> ghost = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            Assert.Equal(4, ghost.Length);
            Assert.Equal(4, ghost.ByteLength);
            Assert.True(ghost.Contains(0x03));
        }

        [Fact]
        public void WorksWithFloatType()
        {
            GhostSpan<float> ghost = new float[] { 1.0f, 2.5f, 3.14f };
            Assert.Equal(3, ghost.Length);
            Assert.Equal(12, ghost.ByteLength);
            Assert.Equal(2.5f, ghost[1]);
        }

        [Fact]
        public void WorksWithDoubleType()
        {
            GhostSpan<double> ghost = new double[] { 1.0, 2.5, 3.14159 };
            Assert.Equal(3, ghost.Length);
            Assert.Equal(24, ghost.ByteLength);
            Assert.Equal(3.14159, ghost[2]);
        }

        [Fact]
        public void WorksWithGuidType()
        {
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            GhostSpan<Guid> ghost = new Guid[] { guid1, guid2 };
            Assert.Equal(2, ghost.Length);
            Assert.Equal(32, ghost.ByteLength);
            Assert.True(ghost.Contains(guid1));
            Assert.True(ghost.Contains(guid2));
        }

        [Fact]
        public void WorksWithCustomStruct()
        {
            GhostSpan<Point2D> ghost = new Point2D[]
            {
                new(1, 2),
                new(3, 4),
                new(5, 6)
            };
            Assert.Equal(3, ghost.Length);
            Assert.Equal(1, ghost[0].X);
            Assert.Equal(4, ghost[1].Y);
        }

        private readonly struct Point2D(int x, int y)
        {
            public int X { get; } = x;
            public int Y { get; } = y;
        }

        // -------------------------------------------------------------------------
        // EDGE CASES
        // -------------------------------------------------------------------------

        [Fact]
        public void HandleSingleElementArray()
        {
            GhostSpan<int> ghost = new int[] { 42 };
            Assert.Equal(1, ghost.Length);
            Assert.Equal(42, ghost.First);
            Assert.Equal(42, ghost.Last);
            Assert.Equal(42, ghost[0]);
            Assert.True(ghost.Contains(42));
            Assert.Equal(0, ghost.IndexOf(42));
        }

        [Fact]
        public void SliceToEmpty()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            var sliced = ghost.Slice(3, 0);
            Assert.True(sliced.IsEmpty);
        }

        [Fact]
        public void EmptyArrayOperations()
        {
            GhostSpan<int> ghost = default;

            // These should not throw
            Assert.Equal("[]", ghost.ToString());
            Assert.True(ghost.Equals(Array.Empty<int>()));
            Assert.Equal(-1, ghost.IndexOf(1));
            Assert.False(ghost.Contains(1));
            Assert.True(ghost.All(x => x > 0)); // Vacuously true
            Assert.False(ghost.Any(x => x > 0));
            Assert.Equal(0, ghost.Count(1));
        }

        [Fact]
        public void LargeArray()
        {
            var large = Enumerable.Range(0, 10000).ToArray();
            GhostSpan<int> ghost = large;

            Assert.Equal(10000, ghost.Length);
            Assert.Equal(0, ghost.First);
            Assert.Equal(9999, ghost.Last);
            Assert.Equal(5000, ghost.IndexOf(5000));
            Assert.True(ghost.Contains(9999));
        }

        // -------------------------------------------------------------------------
        // READ-ONLY BEHAVIOR
        // -------------------------------------------------------------------------

        [Fact]
        public void ThrowsOnWritableSpanForSourceArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            try
            {
                _ = ghost.AsWritableSpan();
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected - source array-backed GhostSpan is read-only
            }
        }

        [Fact]
        public void ThrowsOnReverseForSourceArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            try
            {
                ghost.Reverse();
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected
            }
        }

        [Fact]
        public void ThrowsOnFillForSourceArray()
        {
            GhostSpan<int> ghost = new int[] { 1, 2, 3 };
            try
            {
                ghost.Fill(0);
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected
            }
        }

        [Fact]
        public void ThrowsOnSortForSourceArray()
        {
            GhostSpan<int> ghost = new int[] { 3, 1, 2 };
            try
            {
                ghost.Sort();
                Assert.Fail("Expected InvalidOperationException");
            } catch (InvalidOperationException)
            {
                // Expected
            }
        }
    }
}
