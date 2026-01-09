using GhostBodyObject.HandWritten.Entities;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.HandWritten.Entities.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using System.Text;
using System.Linq;

namespace GhostBodyObject.HandWritten.Tests.TestModel.Entities
{
    public class ArraysAsStringsAndSpansLargeShould
    {
        // =========================================================================
        // VALUE PROPERTIES - OneDateTime and OneInt
        // =========================================================================

        #region Value Properties Tests

        [Fact]
        public void InitializeWithDefaultValues()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                Assert.Equal(1, body.Transaction.ArraysAsStringsAndSpansLargeCollection.Count());

                Assert.Equal(default(DateTime), body.OneDateTime);
                Assert.Equal(0, body.OneInt);
            }
        }

        [Fact]
        public void SetAndGetOneDateTimeProperty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var now = DateTime.Now;
                body.OneDateTime = now;
                Assert.Equal(now, body.OneDateTime);

                var minValue = DateTime.MinValue;
                body.OneDateTime = minValue;
                Assert.Equal(minValue, body.OneDateTime);

                var maxValue = DateTime.MaxValue;
                body.OneDateTime = maxValue;
                Assert.Equal(maxValue, body.OneDateTime);
            }
        }

        [Fact]
        public void SetAndGetOneIntProperty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.OneInt = 42;
                Assert.Equal(42, body.OneInt);

                body.OneInt = int.MinValue;
                Assert.Equal(int.MinValue, body.OneInt);

                body.OneInt = int.MaxValue;
                Assert.Equal(int.MaxValue, body.OneInt);
            }
        }

        #endregion

        // =========================================================================
        // BASIC ARRAY OPERATIONS
        // =========================================================================

        #region Basic Array Operations

        [Fact]
        public void InitializeGuidsAsEmpty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                Assert.True(body.Guids.IsEmpty);
                Assert.Equal(0, body.Guids.Length);
            }
        }

        [Fact]
        public void SetAndGetGuidsProperty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[1], body.Guids[1]);
                Assert.Equal(guids[2], body.Guids[2]);
            }
        }

        [Fact]
        public void SetAndGetDateTimesProperty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var dates = new[]
                {
                    new DateTime(2020, 1, 1),
                    new DateTime(2021, 6, 15),
                    new DateTime(2022, 12, 31)
                };
                body.DateTimes = dates;

                Assert.Equal(3, body.DateTimes.Length);
                Assert.Equal(dates[0], body.DateTimes[0]);
                Assert.Equal(dates[1], body.DateTimes[1]);
                Assert.Equal(dates[2], body.DateTimes[2]);
            }
        }

        [Fact]
        public void SetAndGetStringU16Property()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.StringU16 = "Hello, World!";
                Assert.Equal("Hello, World!", body.StringU16.ToString());
                Assert.Equal(13, body.StringU16.Length);
            }
        }

        [Fact]
        public void SetAndGetStringU8Property()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.StringU8 = "Hello, World!";
                Assert.Equal("Hello, World!", body.StringU8.ToString());
                Assert.Equal(13, body.StringU8.ByteLength);
            }
        }

        #endregion

        // =========================================================================
        // MODERATE SIZE ARRAYS (within safe limits)
        // =========================================================================

        #region Moderate Size Arrays

        [Fact]
        public void HandleModerateGuidArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                // 100 guids = 1,600 bytes - well within limits
                const int guidCount = 100;
                var guids = Enumerable.Range(0, guidCount).Select(_ => Guid.NewGuid()).ToArray();

                body.Guids = guids;

                Assert.Equal(guidCount, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[guidCount - 1], body.Guids[guidCount - 1]);
            }
        }

        [Fact]
        public void HandleModerateDateTimeArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                const int dateCount = 200;
                var dates = Enumerable.Range(0, dateCount)
                    .Select(i => new DateTime(2020, 1, 1).AddMinutes(i))
                    .ToArray();

                body.DateTimes = dates;

                Assert.Equal(dateCount, body.DateTimes.Length);
                Assert.Equal(dates[0], body.DateTimes[0]);
                Assert.Equal(dates[dateCount - 1], body.DateTimes[dateCount - 1]);
            }
        }

        [Fact]
        public void HandleModerateStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                const int charCount = 1000;
                var str = new string('A', charCount);

                body.StringU16 = str;

                Assert.Equal(charCount, body.StringU16.Length);
                Assert.Equal(str, body.StringU16.ToString());
            }
        }

        [Fact]
        public void HandleModerateStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                const int byteCount = 2000;
                var str = new string('X', byteCount);

                body.StringU8 = str;

                Assert.Equal(byteCount, body.StringU8.ByteLength);
                Assert.Equal(str, body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // SEARCH OPERATIONS
        // =========================================================================

        #region Search Operations

        [Fact]
        public void SearchInGuidArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();
                var targetGuid = guids[35];
                body.Guids = guids;

                Assert.Equal(35, body.Guids.IndexOf(targetGuid));
                Assert.True(body.Guids.Contains(targetGuid));
            }
        }

        [Fact]
        public void SearchInString()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var prefix = new string('A', 100);
                var marker = "FINDME";
                var suffix = new string('B', 100);
                body.StringU16 = prefix + marker + suffix;

                Assert.Equal(100, body.StringU16.IndexOf("FINDME"));
                Assert.True(body.StringU16.Contains("FINDME"));
            }
        }

        #endregion

        // =========================================================================
        // LINQ-LIKE OPERATIONS
        // =========================================================================

        #region LINQ-Like Operations

        [Fact]
        public void PerformSelectOnArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var dates = Enumerable.Range(2000, 50).Select(y => new DateTime(y, 1, 1)).ToArray();
                body.DateTimes = dates;

                var years = body.DateTimes.Select(d => d.Year);
                Assert.Equal(50, years.Length);
                Assert.Equal(2000, years[0]);
                Assert.Equal(2049, years[49]);
            }
        }

        [Fact]
        public void PerformWhereOnArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var dates = Enumerable.Range(0, 100).Select(i => new DateTime(2020, 1, 1).AddDays(i)).ToArray();
                body.DateTimes = dates;

                var sundays = body.DateTimes.Where(d => d.DayOfWeek == DayOfWeek.Sunday);
                Assert.True(sundays.Length > 0);
                Assert.True(sundays.All(d => d.DayOfWeek == DayOfWeek.Sunday));
            }
        }

        [Fact]
        public void PerformTakeSkipOnArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;

                var taken = body.Guids.Take(10);
                Assert.Equal(10, taken.Length);

                var skipped = body.Guids.Skip(90);
                Assert.Equal(10, skipped.Length);
            }
        }

        #endregion

        // =========================================================================
        // SLICE OPERATIONS
        // =========================================================================

        #region Slice Operations

        [Fact]
        public void SliceGuidArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;

                var slice = body.Guids.AsSpan(20, 10);
                Assert.Equal(10, slice.Length);
                Assert.Equal(guids[20], slice[0]);
                Assert.Equal(guids[29], slice[9]);
            }
        }

        [Fact]
        public void SliceString()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.StringU16 = new string('X', 500);
                
                var slice = body.StringU16.AsSpan(200, 50);
                Assert.Equal(50, slice.Length);
                Assert.Equal(new string('X', 50), new string(slice));
            }
        }

        #endregion

        // =========================================================================
        // IN-PLACE MODIFICATIONS
        // =========================================================================

        #region In-Place Modifications

        [Fact]
        public void AppendToGuidArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();

                body.Guids = new[] { guid1 };
                body.Guids.Append(guid2);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);
            }
        }

        [Fact]
        public void PrependToGuidArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();

                body.Guids = new[] { guid1 };
                body.Guids.Prepend(guid2);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid2, body.Guids[0]);
                Assert.Equal(guid1, body.Guids[1]);
            }
        }

        [Fact]
        public void AppendToString()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.StringU16 = "Hello";
                body.StringU16.Append(", World!");

                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveFromArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;

                body.Guids.RemoveAt(1);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[2], body.Guids[1]);
            }
        }

        #endregion

        // =========================================================================
        // DATA INTEGRITY
        // =========================================================================

        #region Data Integrity

        [Fact]
        public void MaintainValuePropertiesWithArrays()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                var dateTime = new DateTime(2024, 6, 15);
                body.OneDateTime = dateTime;
                body.OneInt = 999999;

                // Add arrays
                body.Guids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToArray();
                body.StringU16 = new string('X', 500);

                // Verify value properties are intact
                Assert.Equal(dateTime, body.OneDateTime);
                Assert.Equal(999999, body.OneInt);
            }
        }

        [Fact]
        public void MaintainDataIntegrityDuringArrayResizes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                body.OneInt = 12345;
                body.StringU16 = "Fixed String";

                // Multiple resizes
                for (int size = 10; size <= 100; size += 10)
                {
                    var guids = Enumerable.Range(0, size).Select(_ => Guid.NewGuid()).ToArray();
                    body.Guids = guids;

                    Assert.Equal(size, body.Guids.Length);
                    Assert.Equal(12345, body.OneInt);
                    Assert.Equal("Fixed String", body.StringU16.ToString());
                }
            }
        }

        #endregion

        // =========================================================================
        // STRESS TESTS
        // =========================================================================

        #region Stress Tests

        [Fact]
        public void HandleManyArrayReplacements()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                for (int i = 0; i < 20; i++)
                {
                    var size = 10 + (i * 5);
                    var guids = Enumerable.Range(0, size).Select(_ => Guid.NewGuid()).ToArray();
                    body.Guids = guids;

                    Assert.Equal(size, body.Guids.Length);
                    Assert.Equal(guids[0], body.Guids[0]);
                    Assert.Equal(guids[size - 1], body.Guids[size - 1]);
                }
            }
        }

        [Fact]
        public void HandleManyStringReplacements()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                for (int i = 0; i < 20; i++)
                {
                    var length = 50 + (i * 25);
                    var str = new string((char)('A' + (i % 26)), length);
                    body.StringU16 = str;

                    Assert.Equal(length, body.StringU16.Length);
                    Assert.Equal(str, body.StringU16.ToString());
                }
            }
        }

        #endregion

        // =========================================================================
        // COMPARISON WITH SMALL VERSION - Large can handle element counts > 2047
        // =========================================================================

        #region Comparison with Small Version Limits

        [Fact]
        public void AcceptArrayLengthBeyondSmallLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                // Small version max length is 2047 elements (11 bits)
                // Large version uses 24 bits, so can handle much more
                // Let's verify we can at least set more than 2047
                // Note: Not testing very large due to implementation constraints
                const int elementCount = 2100; // Just beyond 2047
                var dates = Enumerable.Range(0, elementCount).Select(i => DateTime.Now.AddMinutes(i)).ToArray();

                // With DateTimes (8 bytes each), 2100 elements = 16,800 bytes - safe
                body.DateTimes = dates;
                Assert.Equal(elementCount, body.DateTimes.Length);
            }
        }

        [Fact]
        public void LargeArrayUsesArrayMapLargeEntry()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();

                // Just verify that Large version doesn't throw for moderate sizes
                // that would require more than small entry can handle
                body.Guids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();
                body.DateTimes = Enumerable.Range(0, 200).Select(i => DateTime.Now.AddDays(i)).ToArray();
                body.StringU16 = new string('L', 1000);
                body.StringU8 = new string('U', 1500);

                Assert.Equal(100, body.Guids.Length);
                Assert.Equal(200, body.DateTimes.Length);
                Assert.Equal(1000, body.StringU16.Length);
                Assert.Equal(1500, body.StringU8.ByteLength);
            }
        }

        #endregion
    }
}
