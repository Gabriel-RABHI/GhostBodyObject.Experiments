using GhostBodyObject.HandWritten.TestModel;
using GhostBodyObject.HandWritten.TestModel.Arrays;
using GhostBodyObject.HandWritten.TestModel.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using System.Text;

namespace GhostBodyObject.HandWritten.Tests.BloggerAll
{
    public class ArraysAsStringsAndSpansSmallShould
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
                var body = new ArraysAsStringsAndSpansSmall();

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
                var body = new ArraysAsStringsAndSpansSmall();

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
                var body = new ArraysAsStringsAndSpansSmall();

                body.OneInt = 42;
                Assert.Equal(42, body.OneInt);

                body.OneInt = int.MinValue;
                Assert.Equal(int.MinValue, body.OneInt);

                body.OneInt = int.MaxValue;
                Assert.Equal(int.MaxValue, body.OneInt);

                body.OneInt = -1;
                Assert.Equal(-1, body.OneInt);

                body.OneInt = 0;
                Assert.Equal(0, body.OneInt);
            }
        }

        [Fact]
        public void MaintainValuePropertiesAfterArrayModifications()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set initial values
                var dateTime = new DateTime(2024, 1, 15, 10, 30, 0);
                body.OneDateTime = dateTime;
                body.OneInt = 12345;

                // Modify arrays
                body.StringU16 = "Test String UTF16";
                body.StringU8 = "Test String UTF8";
                body.Guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                body.DateTimes = new[] { DateTime.Now, DateTime.UtcNow };

                // Verify value properties are unchanged
                Assert.Equal(dateTime, body.OneDateTime);
                Assert.Equal(12345, body.OneInt);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN<GUID> - Guids Property
        // =========================================================================

        #region GhostSpan<Guid> Tests

        [Fact]
        public void InitializeGuidsAsEmpty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

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
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[1], body.Guids[1]);
                Assert.Equal(guids[2], body.Guids[2]);
            }
        }

        [Fact]
        public void ReplaceGuidsMultipleTimes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // First set
                var guids1 = new[] { Guid.NewGuid() };
                body.Guids = guids1;
                Assert.Equal(1, body.Guids.Length);
                Assert.Equal(guids1[0], body.Guids[0]);

                // Replace with more
                var guids2 = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids2;
                Assert.Equal(5, body.Guids.Length);
                for (int i = 0; i < 5; i++)
                    Assert.Equal(guids2[i], body.Guids[i]);

                // Replace with fewer
                var guids3 = new[] { Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids3;
                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guids3[0], body.Guids[0]);
                Assert.Equal(guids3[1], body.Guids[1]);

                // Clear
                body.Guids = Array.Empty<Guid>();
                Assert.True(body.Guids.IsEmpty);
            }
        }

        [Fact]
        public void PerformGuidsSpanReadOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var target = Guid.NewGuid();
                var guids = new[] { Guid.NewGuid(), target, Guid.NewGuid(), target, Guid.NewGuid() };
                body.Guids = guids;

                // IndexOf
                Assert.Equal(1, body.Guids.IndexOf(target));

                // LastIndexOf
                Assert.Equal(3, body.Guids.LastIndexOf(target));

                // Contains
                Assert.True(body.Guids.Contains(target));
                Assert.False(body.Guids.Contains(Guid.NewGuid()));

                // First/Last
                Assert.Equal(guids[0], body.Guids.First);
                Assert.Equal(guids[4], body.Guids.Last);

                // ToArray
                var array = body.Guids.ToArray();
                Assert.Equal(guids, array);
            }
        }

        [Fact]
        public void PerformGuidsSpanSliceOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;

                var span = body.Guids.AsSpan();
                Assert.Equal(5, span.Length);

                var slice = body.Guids.AsSpan(1, 3);
                Assert.Equal(3, slice.Length);
                Assert.Equal(guids[1], slice[0]);
                Assert.Equal(guids[2], slice[1]);
                Assert.Equal(guids[3], slice[2]);
            }
        }

        [Fact]
        public void PerformGuidsSearchOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                body.Guids = new[] { guid1, guid2, guid3, guid1 };

                // StartsWith/EndsWith
                Assert.True(body.Guids.StartsWith(guid1));
                Assert.True(body.Guids.EndsWith(guid1));
                Assert.False(body.Guids.StartsWith(guid2));

                // Count
                Assert.Equal(2, body.Guids.Count(guid1));
                Assert.Equal(1, body.Guids.Count(guid2));
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN<DATETIME> - DateTimes Property
        // =========================================================================

        #region GhostSpan<DateTime> Tests

        [Fact]
        public void InitializeDateTimesAsEmpty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                Assert.True(body.DateTimes.IsEmpty);
                Assert.Equal(0, body.DateTimes.Length);
            }
        }

        [Fact]
        public void SetAndGetDateTimesProperty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

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
        public void PerformDateTimesSpanReadOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var target = new DateTime(2023, 6, 15, 12, 0, 0);
                var dates = new[]
                {
                    DateTime.MinValue,
                    target,
                    DateTime.MaxValue,
                    target,
                    new DateTime(2024, 1, 1)
                };
                body.DateTimes = dates;

                Assert.Equal(1, body.DateTimes.IndexOf(target));
                Assert.Equal(3, body.DateTimes.LastIndexOf(target));
                Assert.True(body.DateTimes.Contains(target));
                Assert.Equal(dates[0], body.DateTimes.First);
                Assert.Equal(dates[4], body.DateTimes.Last);
            }
        }

        [Fact]
        public void ReplaceDateTimesMultipleTimes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Small array
                body.DateTimes = new[] { DateTime.Now };
                Assert.Equal(1, body.DateTimes.Length);

                // Larger array
                var dates = Enumerable.Range(0, 10).Select(i => DateTime.Now.AddDays(i)).ToArray();
                body.DateTimes = dates;
                Assert.Equal(10, body.DateTimes.Length);

                // Empty
                body.DateTimes = Array.Empty<DateTime>();
                Assert.True(body.DateTimes.IsEmpty);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF16 - StringU16 Property
        // =========================================================================

        #region GhostStringUtf16 Tests

        [Fact]
        public void InitializeStringU16AsEmpty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                Assert.True(body.StringU16.IsEmpty);
                Assert.Equal(0, body.StringU16.Length);
                Assert.Equal("", body.StringU16.ToString());
            }
        }

        [Fact]
        public void SetAndGetStringU16Property()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                Assert.Equal("Hello, World!", body.StringU16.ToString());
                Assert.Equal(13, body.StringU16.Length);
            }
        }

        [Fact]
        public void ReplaceStringU16MultipleTimes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "First";
                Assert.Equal("First", body.StringU16.ToString());

                body.StringU16 = "Second String With More Characters";
                Assert.Equal("Second String With More Characters", body.StringU16.ToString());

                body.StringU16 = "Tiny";
                Assert.Equal("Tiny", body.StringU16.ToString());

                body.StringU16 = "";
                Assert.True(body.StringU16.IsEmpty);
            }
        }

        [Fact]
        public void PerformStringU16SearchOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World! Hello, Universe!";

                Assert.Equal(0, body.StringU16.IndexOf('H'));
                Assert.Equal(7, body.StringU16.IndexOf("World"));
                Assert.Equal(14, body.StringU16.IndexOf("Hello", 1));

                Assert.Equal(14, body.StringU16.LastIndexOf("Hello"));
                // "Hello, World! Hello, Universe!" - last 'l' is at position 17 (in second "Hello")
                Assert.Equal(17, body.StringU16.LastIndexOf('l'));

                Assert.True(body.StringU16.Contains("World"));
                Assert.True(body.StringU16.Contains("Universe"));
                Assert.False(body.StringU16.Contains("Galaxy"));

                Assert.True(body.StringU16.StartsWith("Hello"));
                Assert.True(body.StringU16.EndsWith("Universe!"));
            }
        }

        [Fact]
        public void PerformStringU16ComparisonOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";

                Assert.True(body.StringU16 == "Hello");
                Assert.True(body.StringU16 != "World");
                Assert.True(body.StringU16.Equals("Hello"));
                Assert.True(body.StringU16.Equals("hello", StringComparison.OrdinalIgnoreCase));
                Assert.False(body.StringU16.Equals("hello", StringComparison.Ordinal));

                Assert.Equal(0, body.StringU16.CompareTo("Hello"));
                Assert.True(body.StringU16.CompareTo("World") < 0);
                Assert.True(body.StringU16.CompareTo("Apple") > 0);
            }
        }

        [Fact]
        public void HandleUnicodeInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Japanese text
                body.StringU16 = "日本語テスト";
                Assert.Equal("日本語テスト", body.StringU16.ToString());
                Assert.Equal(6, body.StringU16.Length);

                // Emoji
                body.StringU16 = "Hello 👋 World 🌍";
                Assert.True(body.StringU16.Contains("👋"));
                Assert.True(body.StringU16.Contains("🌍"));

                // Mixed content
                body.StringU16 = "Café résumé naïve";
                Assert.True(body.StringU16.Contains("é"));
            }
        }

        [Fact]
        public void PerformStringU16StringManipulations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "  Hello, World!  ";

                // Trim (returns new string, doesn't modify in place)
                Assert.Equal("Hello, World!", body.StringU16.Trim());
                Assert.Equal("Hello, World!  ", body.StringU16.TrimStart());
                Assert.Equal("  Hello, World!", body.StringU16.TrimEnd());

                // Case conversion (returns new string)
                body.StringU16 = "Hello";
                Assert.Equal("HELLO", body.StringU16.ToUpper());
                Assert.Equal("hello", body.StringU16.ToLower());

                // Replace (returns new string)
                body.StringU16 = "Hello, World!";
                Assert.Equal("Hello, Universe!", body.StringU16.Replace("World", "Universe"));

                // Substring
                Assert.Equal("World!", body.StringU16.Substring(7));
                Assert.Equal("World", body.StringU16.Substring(7, 5));
            }
        }

        [Fact]
        public void PerformStringU16SliceOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";

                var span = body.StringU16.AsSpan();
                Assert.Equal("Hello, World!", new string(span));

                var slice = body.StringU16.AsSpan(7, 5);
                Assert.Equal("World", new string(slice));

                var sliced = body.StringU16.Slice(7);
                Assert.Equal("World!", sliced.ToString());

                var slicedWithLength = body.StringU16.Slice(7, 5);
                Assert.Equal("World", slicedWithLength.ToString());
            }
        }

        [Fact]
        public void ConvertStringU16ToUtf8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                var utf8Bytes = body.StringU16.ToUtf8Bytes();
                Assert.Equal(Encoding.UTF8.GetBytes("Hello"), utf8Bytes);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - StringU8 Property
        // =========================================================================

        #region GhostStringUtf8 Tests

        [Fact]
        public void InitializeStringU8AsEmpty()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                Assert.True(body.StringU8.IsEmpty);
                Assert.Equal(0, body.StringU8.ByteLength);
                Assert.Equal("", body.StringU8.ToString());
            }
        }

        [Fact]
        public void SetAndGetStringU8Property()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World!";
                Assert.Equal("Hello, World!", body.StringU8.ToString());
                Assert.Equal(13, body.StringU8.ByteLength); // ASCII chars = 1 byte each
            }
        }

        [Fact]
        public void ReplaceStringU8MultipleTimes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "First";
                Assert.Equal("First", body.StringU8.ToString());

                body.StringU8 = "Second String With More Characters";
                Assert.Equal("Second String With More Characters", body.StringU8.ToString());

                body.StringU8 = "Tiny";
                Assert.Equal("Tiny", body.StringU8.ToString());

                body.StringU8 = "";
                Assert.True(body.StringU8.IsEmpty);
            }
        }

        [Fact]
        public void PerformStringU8SearchOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World! Hello, Universe!";

                Assert.Equal(0, body.StringU8.IndexOf('H'));
                Assert.Equal(7, body.StringU8.IndexOf("World"));

                Assert.True(body.StringU8.Contains("World"));
                Assert.True(body.StringU8.Contains("Universe"));
                Assert.False(body.StringU8.Contains("Galaxy"));

                Assert.True(body.StringU8.StartsWith("Hello"));
                Assert.True(body.StringU8.EndsWith("Universe!"));
            }
        }

        [Fact]
        public void PerformStringU8ComparisonOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";

                Assert.True(body.StringU8 == "Hello");
                Assert.True(body.StringU8 != "World");
                Assert.True(body.StringU8.Equals("Hello"));
            }
        }

        [Fact]
        public void HandleUnicodeInStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Multi-byte UTF-8 characters
                body.StringU8 = "日本語";
                Assert.Equal("日本語", body.StringU8.ToString());
                Assert.Equal(9, body.StringU8.ByteLength); // 3 characters * 3 bytes each

                // Café with accented characters
                body.StringU8 = "Café";
                Assert.Equal("Café", body.StringU8.ToString());
                Assert.Equal(5, body.StringU8.ByteLength); // C(1) + a(1) + f(1) + é(2) = 5
            }
        }

        [Fact]
        public void PerformStringU8ByteOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";

                var bytes = body.StringU8.AsBytes();
                Assert.Equal(5, bytes.Length);
                Assert.Equal((byte)'H', bytes[0]);
                Assert.Equal((byte)'e', bytes[1]);
                Assert.Equal((byte)'o', bytes[4]);
            }
        }

        [Fact]
        public void VerifyUtf8ByteEncoding()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var testString = "Hello, World! 日本語";

                body.StringU8 = testString;
                var expectedBytes = Encoding.UTF8.GetBytes(testString);

                Assert.Equal(expectedBytes.Length, body.StringU8.ByteLength);
                Assert.Equal(expectedBytes, body.StringU8.AsBytes().ToArray());
            }
        }

        #endregion

        // =========================================================================
        // DATA INTEGRITY - Ensuring modifications don't corrupt other data
        // =========================================================================

        #region Data Integrity Tests

        [Fact]
        public void MaintainDataIntegrityWhenModifyingAllProperties()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set all properties
                var dateTime = new DateTime(2024, 6, 15, 10, 30, 0);
                body.OneDateTime = dateTime;
                body.OneInt = 42;
                body.StringU16 = "UTF16 String";
                body.StringU8 = "UTF8 String";
                body.Guids = new[] { Guid.Parse("12345678-1234-1234-1234-123456789012") };
                body.DateTimes = new[] { new DateTime(2020, 1, 1) };

                // Verify all values
                Assert.Equal(dateTime, body.OneDateTime);
                Assert.Equal(42, body.OneInt);
                Assert.Equal("UTF16 String", body.StringU16.ToString());
                Assert.Equal("UTF8 String", body.StringU8.ToString());
                Assert.Equal(Guid.Parse("12345678-1234-1234-1234-123456789012"), body.Guids[0]);
                Assert.Equal(new DateTime(2020, 1, 1), body.DateTimes[0]);
            }
        }

        [Fact]
        public void MaintainDataIntegrityAfterMultipleArrayResizes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set initial values
                body.OneInt = 999;
                body.StringU16 = "Initial";

                // Multiple resize operations on different arrays
                for (int i = 0; i < 10; i++)
                {
                    body.StringU8 = new string('X', i * 10);
                    body.Guids = Enumerable.Range(0, i + 1).Select(_ => Guid.NewGuid()).ToArray();
                    body.DateTimes = Enumerable.Range(0, i * 2).Select(j => DateTime.Now.AddDays(j)).ToArray();

                    // Value properties should remain intact
                    Assert.Equal(999, body.OneInt);
                    Assert.Equal("Initial", body.StringU16.ToString());
                }
            }
        }

        [Fact]
        public void MaintainDataIntegrityWithLargeStrings()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set value properties
                body.OneInt = 777;
                body.OneDateTime = DateTime.Now;

                // Large string (staying within ArrayMapSmallEntry limits)
                var largeString = new string('A', 500);
                body.StringU16 = largeString;

                Assert.Equal(largeString, body.StringU16.ToString());
                Assert.Equal(777, body.OneInt);

                // Replace with different size
                var smallString = "Tiny";
                body.StringU16 = smallString;

                Assert.Equal(smallString, body.StringU16.ToString());
                Assert.Equal(777, body.OneInt);
            }
        }

        [Fact]
        public void MaintainDataIntegrityWithInterleaveModifications()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                for (int i = 0; i < 20; i++)
                {
                    // Interleave modifications to all properties
                    body.OneInt = i;
                    body.StringU16 = $"String {i}";
                    body.Guids = new[] { Guid.NewGuid() };
                    body.StringU8 = $"UTF8 {i}";
                    body.OneDateTime = DateTime.Now.AddDays(i);
                    body.DateTimes = new[] { DateTime.Now };

                    // Verify
                    Assert.Equal(i, body.OneInt);
                    Assert.Equal($"String {i}", body.StringU16.ToString());
                    Assert.Equal($"UTF8 {i}", body.StringU8.ToString());
                    Assert.Equal(1, body.Guids.Length);
                    Assert.Equal(1, body.DateTimes.Length);
                }
            }
        }

        [Fact]
        public void MaintainAllPropertiesAfterStringChanges()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set up all properties
                var guid = Guid.NewGuid();
                var date = new DateTime(2024, 6, 15);
                body.OneInt = 42;
                body.OneDateTime = date;
                body.Guids = new[] { guid };
                body.DateTimes = new[] { date };
                body.StringU16 = "Initial UTF16";
                body.StringU8 = "Initial UTF8";

                // Make multiple string changes of varying sizes
                for (int i = 0; i < 50; i++)
                {
                    body.StringU16 = new string((char)('A' + (i % 26)), (i % 100) + 1);
                    body.StringU8 = new string((char)('a' + (i % 26)), (i % 50) + 1);
                }

                // Verify other properties are still intact
                Assert.Equal(42, body.OneInt);
                Assert.Equal(date, body.OneDateTime);
                Assert.Equal(guid, body.Guids[0]);
                Assert.Equal(date, body.DateTimes[0]);
            }
        }

        #endregion

        // =========================================================================
        // STRESS TESTS - High-volume operations
        // =========================================================================

        #region Stress Tests

        [Fact]
        public void HandleRapidReplacementOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                for (int i = 0; i < 100; i++)
                {
                    var length = (i % 50) + 1;
                    body.StringU16 = new string((char)('A' + (i % 26)), length);
                    Assert.Equal(length, body.StringU16.Length);
                }
            }
        }

        [Fact]
        public void HandleManyGuidReplacements()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                for (int i = 0; i < 50; i++)
                {
                    var count = (i % 10) + 1;
                    var guids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
                    body.Guids = guids;

                    Assert.Equal(count, body.Guids.Length);
                    for (int j = 0; j < count; j++)
                    {
                        Assert.Equal(guids[j], body.Guids[j]);
                    }
                }
            }
        }

        [Fact]
        public void HandleAlternatingEmptyAndFilledArrays()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                for (int i = 0; i < 20; i++)
                {
                    if (i % 2 == 0)
                    {
                        body.StringU16 = "Some content";
                        body.Guids = new[] { Guid.NewGuid() };
                    }
                    else
                    {
                        body.StringU16 = "";
                        body.Guids = Array.Empty<Guid>();
                    }

                    if (i % 2 == 0)
                    {
                        Assert.False(body.StringU16.IsEmpty);
                        Assert.False(body.Guids.IsEmpty);
                    }
                    else
                    {
                        Assert.True(body.StringU16.IsEmpty);
                        Assert.True(body.Guids.IsEmpty);
                    }
                }
            }
        }

        #endregion

        // =========================================================================
        // EDGE CASES - Boundary conditions and special values
        // =========================================================================

        #region Edge Cases

        [Fact]
        public void HandleEmptyArrayAssignment()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Assign non-empty, then empty
                body.Guids = new[] { Guid.NewGuid() };
                body.Guids = Array.Empty<Guid>();
                Assert.True(body.Guids.IsEmpty);

                body.DateTimes = new[] { DateTime.Now };
                body.DateTimes = Array.Empty<DateTime>();
                Assert.True(body.DateTimes.IsEmpty);
            }
        }

        [Fact]
        public void HandleEmptyStringAssignment()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Not Empty";
                body.StringU16 = "";
                Assert.True(body.StringU16.IsEmpty);

                body.StringU8 = "Not Empty";
                body.StringU8 = "";
                Assert.True(body.StringU8.IsEmpty);
            }
        }

        [Fact]
        public void HandleSpecialCharactersInStrings()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Null characters
                body.StringU16 = "Hello\0World";
                Assert.Equal("Hello\0World", body.StringU16.ToString());

                // Tab and newline
                body.StringU16 = "Line1\tTab\nLine2";
                Assert.True(body.StringU16.Contains('\t'));
                Assert.True(body.StringU16.Contains('\n'));

                // High Unicode
                body.StringU16 = "🎉🎊🎁";
                Assert.True(body.StringU16.Length > 0);
            }
        }

        [Fact]
        public void HandleSingleElementArrays()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var singleGuid = Guid.NewGuid();
                body.Guids = new[] { singleGuid };
                Assert.Equal(1, body.Guids.Length);
                Assert.Equal(singleGuid, body.Guids.First);
                Assert.Equal(singleGuid, body.Guids.Last);
                Assert.Equal(singleGuid, body.Guids[0]);

                var singleDate = DateTime.Now;
                body.DateTimes = new[] { singleDate };
                Assert.Equal(1, body.DateTimes.Length);
                Assert.Equal(singleDate, body.DateTimes.First);
            }
        }

        [Fact]
        public void HandleSingleCharacterStrings()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "X";
                Assert.Equal(1, body.StringU16.Length);
                Assert.Equal('X', body.StringU16.First);
                Assert.Equal('X', body.StringU16.Last);

                body.StringU8 = "Y";
                Assert.Equal(1, body.StringU8.ByteLength);
            }
        }

        #endregion

        // =========================================================================
        // READ-BACK VERIFICATION - Zero-copy access patterns
        // =========================================================================

        #region Read-Back Verification

        [Fact]
        public void VerifyConsistentReadBackForStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Test String For Read Consistency";

                // Multiple reads should return consistent data
                for (int i = 0; i < 1000; i++)
                {
                    Assert.Equal("Test String For Read Consistency", body.StringU16.ToString());
                    Assert.True(body.StringU16.Equals("Test String For Read Consistency"));
                }
            }
        }

        [Fact]
        public void VerifyConsistentReadBackForGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var expectedGuids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = expectedGuids;

                // Multiple reads
                for (int i = 0; i < 1000; i++)
                {
                    Assert.Equal(3, body.Guids.Length);
                    Assert.Equal(expectedGuids[0], body.Guids[0]);
                    Assert.Equal(expectedGuids[1], body.Guids[1]);
                    Assert.Equal(expectedGuids[2], body.Guids[2]);
                }
            }
        }

        [Fact]
        public void VerifySpanEquality()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;

                // Span should equal the original array
                Assert.True(body.Guids.Equals(guids));
                Assert.True(body.Guids == guids);
            }
        }

        [Fact]
        public void VerifyStringSpanEquality()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                Assert.True(body.StringU16 == "Hello");
                Assert.True(body.StringU16.Equals("Hello".AsSpan()));
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN LINQ-LIKE OPERATIONS
        // =========================================================================

        #region GhostSpan LINQ-Like Operations

        [Fact]
        public void PerformGhostSpanAllAnyOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var dates = new[]
                {
                    new DateTime(2020, 1, 1),
                    new DateTime(2021, 1, 1),
                    new DateTime(2022, 1, 1)
                };
                body.DateTimes = dates;

                Assert.True(body.DateTimes.All(d => d.Year >= 2020));
                Assert.False(body.DateTimes.All(d => d.Year == 2020));

                Assert.True(body.DateTimes.Any(d => d.Year == 2021));
                Assert.False(body.DateTimes.Any(d => d.Year == 2025));
            }
        }

        [Fact]
        public void PerformGhostSpanFindOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var target = Guid.NewGuid();
                body.Guids = new[] { Guid.NewGuid(), target, Guid.NewGuid() };

                var found = body.Guids.Find(g => g == target);
                Assert.Equal(target, found);

                var index = body.Guids.FindIndex(g => g == target);
                Assert.Equal(1, index);
            }
        }

        [Fact]
        public void PerformGhostSpanSelectWhereOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var dates = new[]
                {
                    new DateTime(2020, 1, 1),
                    new DateTime(2021, 6, 15),
                    new DateTime(2022, 12, 31)
                };
                body.DateTimes = dates;

                var years = body.DateTimes.Select(d => d.Year);
                Assert.Equal(new[] { 2020, 2021, 2022 }, years);

                var filtered = body.DateTimes.Where(d => d.Year >= 2021);
                Assert.Equal(2, filtered.Length);
            }
        }

        [Fact]
        public void PerformGhostSpanTakeSkipOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;

                var taken = body.Guids.Take(3);
                Assert.Equal(3, taken.Length);
                Assert.Equal(guids.Take(3).ToArray(), taken);

                var skipped = body.Guids.Skip(2);
                Assert.Equal(3, skipped.Length);
                Assert.Equal(guids.Skip(2).ToArray(), skipped);
            }
        }

        #endregion

        // =========================================================================
        // IN-PLACE ARRAY MODIFICATIONS - Append, Prepend, Insert, Remove
        // =========================================================================

        #region In-Place Array Modification Tests

        [Fact]
        public void AppendToGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();

                body.Guids = new[] { guid1 };
                Assert.Equal(1, body.Guids.Length);

                body.Guids.Append(guid2);
                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);

                body.Guids.Append(guid3);
                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guid3, body.Guids[2]);
            }
        }

        [Fact]
        public void PrependToGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();

                body.Guids = new[] { guid1 };
                body.Guids.Prepend(guid2);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid2, body.Guids[0]);
                Assert.Equal(guid1, body.Guids[1]);

                body.Guids.Prepend(guid3);
                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guid3, body.Guids[0]);
            }
        }

        [Fact]
        public void InsertIntoGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();

                body.Guids = new[] { guid1, guid3 };
                body.Guids.InsertAt(1, guid2);

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);
                Assert.Equal(guid3, body.Guids[2]);
            }
        }

        [Fact]
        public void RemoveFromGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;

                body.Guids.RemoveAt(1);
                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[2], body.Guids[1]);
                Assert.Equal(guids[3], body.Guids[2]);
            }
        }

        [Fact]
        public void RemoveRangeFromGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;

                body.Guids.RemoveRange(1, 2);
                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[3], body.Guids[1]);
                Assert.Equal(guids[4], body.Guids[2]);
            }
        }

        [Fact]
        public void ClearGuidsArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.Guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                Assert.Equal(2, body.Guids.Length);

                body.Guids.Clear();
                Assert.True(body.Guids.IsEmpty);
            }
        }

        [Fact]
        public void AppendToDateTimesArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var date1 = new DateTime(2020, 1, 1);
                var date2 = new DateTime(2021, 6, 15);

                body.DateTimes = new[] { date1 };
                body.DateTimes.Append(date2);

                Assert.Equal(2, body.DateTimes.Length);
                Assert.Equal(date1, body.DateTimes[0]);
                Assert.Equal(date2, body.DateTimes[1]);
            }
        }

        [Fact]
        public void RemoveFromDateTimesArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var dates = new[]
                {
                    new DateTime(2020, 1, 1),
                    new DateTime(2021, 1, 1),
                    new DateTime(2022, 1, 1)
                };
                body.DateTimes = dates;

                body.DateTimes.RemoveAt(1);
                Assert.Equal(2, body.DateTimes.Length);
                Assert.Equal(dates[0], body.DateTimes[0]);
                Assert.Equal(dates[2], body.DateTimes[1]);
            }
        }

        #endregion

        // =========================================================================
        // IN-PLACE STRING MODIFICATIONS - Append, Prepend, Insert, Remove
        // =========================================================================

        #region In-Place String Modification Tests

        [Fact]
        public void AppendToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                body.StringU16.Append(", World!");

                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void PrependToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "World";
                body.StringU16.Prepend("Hello, ");

                Assert.Equal("Hello, World", body.StringU16.ToString());
            }
        }

        [Fact]
        public void InsertIntoStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello!";
                body.StringU16.InsertAt(5, " World");

                Assert.Equal("Hello World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                body.StringU16.RemoveAt(5, 7); // Remove ", World"

                Assert.Equal("Hello!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceRangeInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                body.StringU16.ReplaceRange(7, 5, "Universe"); // Replace "World" with "Universe"

                Assert.Equal("Hello, Universe!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ClearStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Some content";
                body.StringU16.Clear();

                Assert.True(body.StringU16.IsEmpty);
            }
        }

        [Fact]
        public void TrimStringU16InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "  Hello  ";
                body.StringU16.TrimInPlace();

                Assert.Equal("Hello", body.StringU16.ToString());
            }
        }

        [Fact]
        public void TrimStartStringU16InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "  Hello  ";
                body.StringU16.TrimStartInPlace();

                Assert.Equal("Hello  ", body.StringU16.ToString());
            }
        }

        [Fact]
        public void TrimEndStringU16InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "  Hello  ";
                body.StringU16.TrimEndInPlace();

                Assert.Equal("  Hello", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ToUpperStringU16InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello World";
                body.StringU16.ToUpperInPlace();

                Assert.Equal("HELLO WORLD", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ToLowerStringU16InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello World";
                body.StringU16.ToLowerInPlace();

                Assert.Equal("hello world", body.StringU16.ToString());
            }
        }

        [Fact]
        public void AppendToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";
                body.StringU8.Append(", World!");

                Assert.Equal("Hello, World!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void PrependToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "World";
                body.StringU8.Prepend("Hello, ");

                Assert.Equal("Hello, World", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ClearStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Some content";
                body.StringU8.Clear();

                Assert.True(body.StringU8.IsEmpty);
            }
        }

        [Fact]
        public void ToUpperStringU8InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello World";
                body.StringU8.ToUpperAsciiInPlace();

                Assert.Equal("HELLO WORLD", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ToLowerStringU8InPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello World";
                body.StringU8.ToLowerAsciiInPlace();

                Assert.Equal("hello world", body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // DATA INTEGRITY DURING IN-PLACE MODIFICATIONS
        // =========================================================================

        #region Data Integrity During In-Place Modifications

        [Fact]
        public void MaintainDataIntegrityDuringStringAppend()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set up all properties
                var guid = Guid.NewGuid();
                var date = new DateTime(2024, 6, 15);
                body.OneInt = 42;
                body.OneDateTime = date;
                body.Guids = new[] { guid };
                body.DateTimes = new[] { date };
                body.StringU16 = "Initial";
                body.StringU8 = "UTF8";

                // Perform multiple append operations
                for (int i = 0; i < 10; i++)
                {
                    body.StringU16.Append(" Appended");
                }

                // Verify other properties are still intact
                Assert.Equal(42, body.OneInt);
                Assert.Equal(date, body.OneDateTime);
                Assert.Equal(guid, body.Guids[0]);
                Assert.Equal(date, body.DateTimes[0]);
                Assert.Equal("UTF8", body.StringU8.ToString());
            }
        }

        [Fact]
        public void MaintainDataIntegrityDuringArrayAppend()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set up all properties
                body.OneInt = 999;
                body.StringU16 = "Test String";
                body.StringU8 = "UTF8 Test";
                body.Guids = Array.Empty<Guid>();

                // Perform multiple append operations
                var appendedGuids = new List<Guid>();
                for (int i = 0; i < 15; i++)
                {
                    var newGuid = Guid.NewGuid();
                    appendedGuids.Add(newGuid);
                    body.Guids.Append(newGuid);
                }

                // Verify all properties
                Assert.Equal(999, body.OneInt);
                Assert.Equal("Test String", body.StringU16.ToString());
                Assert.Equal("UTF8 Test", body.StringU8.ToString());
                Assert.Equal(15, body.Guids.Length);
                for (int i = 0; i < 15; i++)
                {
                    Assert.Equal(appendedGuids[i], body.Guids[i]);
                }
            }
        }

        [Fact]
        public void MaintainDataIntegrityDuringMixedOperations()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Set up
                body.OneInt = 123;
                body.StringU16 = "Hello";
                body.Guids = new[] { Guid.NewGuid() };

                // Interleave operations
                body.StringU16.Append(" World");
                body.Guids.Append(Guid.NewGuid());
                body.StringU16.Prepend("Say: ");
                body.Guids.Prepend(Guid.NewGuid());

                // Verify
                Assert.Equal(123, body.OneInt);
                Assert.Equal("Say: Hello World", body.StringU16.ToString());
                Assert.Equal(3, body.Guids.Length);
            }
        }

        #endregion

        // =========================================================================
        // SMALL ARRAY LIMITS - Overflow Protection Tests
        // =========================================================================

        #region Small Array Limits Tests

        [Fact]
        public void ThrowOverflowExceptionWhenArrayLengthExceedsLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Max array length for small is 2047 elements (11 bits)
                // Try to set 2048 or more elements
                var tooManyGuids = Enumerable.Range(0, 2048).Select(_ => Guid.NewGuid()).ToArray();

                Assert.Throws<OverflowException>(() => body.Guids = tooManyGuids);
            }
        }

        [Fact]
        public void AllowArrayLengthAtMaxLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Max array length for small is 2047 elements - should work
                // 2047 * 16 bytes (Guid) = 32,752 bytes - within offset limit
                var maxGuids = Enumerable.Range(0, 2047).Select(_ => Guid.NewGuid()).ToArray();

                // This should not throw
                body.Guids = maxGuids;
                Assert.Equal(2047, body.Guids.Length);
            }
        }

        [Fact]
        public void ThrowOverflowExceptionWhenStringLengthExceedsLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Max array length for small is 2047 elements
                // For UTF-16 strings, each char is 1 element
                // 2048 chars should overflow the element limit
                var tooLongString = new string('X', 2048);

                Assert.Throws<OverflowException>(() => body.StringU16 = tooLongString);
            }
        }

        [Fact]
        public void AllowStringLengthAtMaxLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Max 2047 characters for UTF-16 string
                var maxString = new string('Y', 2047);

                // This should not throw
                body.StringU16 = maxString;
                Assert.Equal(2047, body.StringU16.Length);
            }
        }

        [Fact]
        public void AllowStringU8ByteLengthAtMaxLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // For UTF-8 strings, each ASCII byte is 1 element
                // Max 2047 bytes
                var maxString = new string('Z', 2047);

                // This should not throw
                body.StringU8 = maxString;
                Assert.Equal(2047, body.StringU8.ByteLength);
            }
        }

        [Fact]
        public void ThrowOverflowExceptionOnAppendExceedingElementLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Start near the element limit
                body.StringU16 = new string('A', 2040);

                // Append enough to exceed the 2047 element limit
                Assert.Throws<OverflowException>(() => 
                    body.StringU16.Append(new string('B', 10)));
            }
        }

        [Fact]
        public void ThrowOverflowExceptionOnGuidAppendExceedingElementLimit()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Start at max-1 elements
                body.Guids = Enumerable.Range(0, 2047).Select(_ => Guid.NewGuid()).ToArray();

                // Try to append one more - should fail
                Assert.Throws<OverflowException>(() => 
                    body.Guids.Append(Guid.NewGuid()));
            }
        }

        [Fact]
        public void VerifySmallArrayMaxConstants()
        {
            // Verify the constants are correctly defined
            Assert.Equal(65535, BodyBase.SmallArrayMaxOffset);
            Assert.Equal(2047, BodyBase.SmallArrayMaxLength);
        }

        [Fact]
        public void AllowModerateArraySizes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                // Test moderate sizes that are well within limits
                body.Guids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToArray();
                Assert.Equal(100, body.Guids.Length);

                body.DateTimes = Enumerable.Range(0, 200).Select(i => DateTime.Now.AddMinutes(i)).ToArray();
                Assert.Equal(200, body.DateTimes.Length);

                body.StringU16 = new string('X', 500);
                Assert.Equal(500, body.StringU16.Length);

                body.StringU8 = new string('Y', 1000);
                Assert.Equal(1000, body.StringU8.ByteLength);
            }
        }

        #endregion
    }
}
