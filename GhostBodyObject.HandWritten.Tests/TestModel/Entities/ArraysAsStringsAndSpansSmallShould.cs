using GhostBodyObject.HandWritten.Entities;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.HandWritten.Entities.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using System.Text;

namespace GhostBodyObject.HandWritten.Tests.TestModel.Entities
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
        // GHOSTSPAN - SETARRAY METHODS
        // =========================================================================

        #region GhostSpan SetArray Tests

        [Fact]
        public void SetArrayGuidsFromArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.Guids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids.SetArray(newGuids);

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(newGuids[0], body.Guids[0]);
                Assert.Equal(newGuids[1], body.Guids[1]);
                Assert.Equal(newGuids[2], body.Guids[2]);
            }
        }

        [Fact]
        public void SetArrayGuidsFromReadOnlySpan()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.Guids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                body.Guids.SetArray(newGuids.AsSpan());

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(newGuids[0], body.Guids[0]);
                Assert.Equal(newGuids[1], body.Guids[1]);
            }
        }

        [Fact]
        public void SetArrayDateTimesFromGhostSpan()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var dates1 = new[] { new DateTime(2020, 1, 1), new DateTime(2021, 1, 1) };
                var dates2 = new[] { new DateTime(2022, 1, 1), new DateTime(2023, 1, 1), new DateTime(2024, 1, 1) };
                
                body.DateTimes = dates1;
                Assert.Equal(2, body.DateTimes.Length);
                
                // Create a GhostSpan from array (implicit conversion)
                Repository.Ghost.Values.GhostSpan<DateTime> sourceSpan = dates2;
                body.DateTimes.SetArray(sourceSpan);

                Assert.Equal(3, body.DateTimes.Length);
                Assert.Equal(dates2[0], body.DateTimes[0]);
                Assert.Equal(dates2[1], body.DateTimes[1]);
                Assert.Equal(dates2[2], body.DateTimes[2]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - APPENDRANGE/PREPENDRANGE METHODS
        // =========================================================================

        #region GhostSpan AppendRange/PrependRange Tests

        [Fact]
        public void AppendRangeToGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var initialGuids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                
                body.Guids = initialGuids;
                body.Guids.AppendRange(newGuids.AsSpan());

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(initialGuids[0], body.Guids[0]);
                Assert.Equal(newGuids[0], body.Guids[1]);
                Assert.Equal(newGuids[1], body.Guids[2]);
            }
        }

        [Fact]
        public void AppendRangeFromGhostSpanToGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var initialGuids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                Repository.Ghost.Values.GhostSpan<Guid> toAppend = newGuids;
                
                body.Guids = initialGuids;
                body.Guids.AppendRange(toAppend);

                Assert.Equal(3, body.Guids.Length);
            }
        }

        [Fact]
        public void PrependRangeToGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var initialGuids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                
                body.Guids = initialGuids;
                body.Guids.PrependRange(newGuids.AsSpan());

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(newGuids[0], body.Guids[0]);
                Assert.Equal(newGuids[1], body.Guids[1]);
                Assert.Equal(initialGuids[0], body.Guids[2]);
            }
        }

        [Fact]
        public void PrependRangeFromGhostSpanToGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var initialGuids = new[] { Guid.NewGuid() };
                var newGuids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                Repository.Ghost.Values.GhostSpan<Guid> toPrepend = newGuids;
                
                body.Guids = initialGuids;
                body.Guids.PrependRange(toPrepend);

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(newGuids[0], body.Guids[0]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - INSERTRANGEAT METHODS
        // =========================================================================

        #region GhostSpan InsertRangeAt Tests

        [Fact]
        public void InsertRangeAtGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                var guid4 = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid4 };
                body.Guids.InsertRangeAt(1, new[] { guid2, guid3 }.AsSpan());

                Assert.Equal(4, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);
                Assert.Equal(guid3, body.Guids[2]);
                Assert.Equal(guid4, body.Guids[3]);
            }
        }

        [Fact]
        public void InsertRangeAtFromGhostSpan()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var date1 = new DateTime(2020, 1, 1);
                var date2 = new DateTime(2021, 1, 1);
                var date3 = new DateTime(2022, 1, 1);
                var date4 = new DateTime(2023, 1, 1);
                
                Repository.Ghost.Values.GhostSpan<DateTime> toInsert = new[] { date2, date3 };
                
                body.DateTimes = new[] { date1, date4 };
                body.DateTimes.InsertRangeAt(1, toInsert);

                Assert.Equal(4, body.DateTimes.Length);
                Assert.Equal(date1, body.DateTimes[0]);
                Assert.Equal(date2, body.DateTimes[1]);
                Assert.Equal(date3, body.DateTimes[2]);
                Assert.Equal(date4, body.DateTimes[3]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - REMOVE METHODS
        // =========================================================================

        #region GhostSpan Remove Tests

        [Fact]
        public void RemoveFromGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var targetGuid = Guid.NewGuid();
                var otherGuid1 = Guid.NewGuid();
                var otherGuid2 = Guid.NewGuid();
                
                body.Guids = new[] { otherGuid1, targetGuid, otherGuid2 };
                bool removed = body.Guids.Remove(targetGuid);

                Assert.True(removed);
                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(otherGuid1, body.Guids[0]);
                Assert.Equal(otherGuid2, body.Guids[1]);
            }
        }

        [Fact]
        public void RemoveNotFoundFromGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var notInArray = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2 };
                bool removed = body.Guids.Remove(notInArray);

                Assert.False(removed);
                Assert.Equal(2, body.Guids.Length);
            }
        }

        [Fact]
        public void RemoveAllFromGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var duplicateGuid = Guid.NewGuid();
                var otherGuid = Guid.NewGuid();
                
                body.Guids = new[] { duplicateGuid, otherGuid, duplicateGuid, duplicateGuid };
                int removedCount = body.Guids.RemoveAll(duplicateGuid);

                Assert.Equal(3, removedCount);
                Assert.Equal(1, body.Guids.Length);
                Assert.Equal(otherGuid, body.Guids[0]);
            }
        }

        [Fact]
        public void PopFromGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2, guid3 };
                var popped = body.Guids.Pop();

                Assert.Equal(guid3, popped);
                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);
            }
        }

        [Fact]
        public void ShiftFromGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2, guid3 };
                var shifted = body.Guids.Shift();

                Assert.Equal(guid1, shifted);
                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guid2, body.Guids[0]);
                Assert.Equal(guid3, body.Guids[1]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - REPLACERANGE METHODS
        // =========================================================================

        #region GhostSpan ReplaceRange Tests

        [Fact]
        public void ReplaceRangeInGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                var newGuid1 = Guid.NewGuid();
                var newGuid2 = Guid.NewGuid();
                var newGuid3 = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2, guid3 };
                body.Guids.ReplaceRange(1, 1, new[] { newGuid1, newGuid2, newGuid3 }.AsSpan());

                Assert.Equal(5, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(newGuid1, body.Guids[1]);
                Assert.Equal(newGuid2, body.Guids[2]);
                Assert.Equal(newGuid3, body.Guids[3]);
                Assert.Equal(guid3, body.Guids[4]);
            }
        }

        [Fact]
        public void ReplaceRangeFromGhostSpanInGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var date1 = new DateTime(2020, 1, 1);
                var date2 = new DateTime(2021, 1, 1);
                var date3 = new DateTime(2022, 1, 1);
                var newDate = new DateTime(2025, 1, 1);
                
                Repository.Ghost.Values.GhostSpan<DateTime> replacement = new[] { newDate };
                
                body.DateTimes = new[] { date1, date2, date3 };
                body.DateTimes.ReplaceRange(1, 1, replacement);

                Assert.Equal(3, body.DateTimes.Length);
                Assert.Equal(date1, body.DateTimes[0]);
                Assert.Equal(newDate, body.DateTimes[1]);
                Assert.Equal(date3, body.DateTimes[2]);
            }
        }

        [Fact]
        public void ReplaceRangeWithSmallerReplacement()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
                var newGuid = Guid.NewGuid();
                
                body.Guids = guids;
                body.Guids.ReplaceRange(1, 3, new[] { newGuid }.AsSpan()); // Replace 3 with 1

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(newGuid, body.Guids[1]);
                Assert.Equal(guids[4], body.Guids[2]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - RESIZE METHOD
        // =========================================================================

        #region GhostSpan Resize Tests

        [Fact]
        public void ResizeGuidsToLarger()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                body.Guids = new[] { guid1 };
                body.Guids.Resize(5);

                Assert.Equal(5, body.Guids.Length);
                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(Guid.Empty, body.Guids[1]); // New elements are default
                Assert.Equal(Guid.Empty, body.Guids[4]);
            }
        }

        [Fact]
        public void ResizeGuidsToSmaller()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
                body.Guids = guids;
                body.Guids.Resize(2);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[1], body.Guids[1]);
            }
        }

        [Fact]
        public void ResizeGuidsToSameSize()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
                body.Guids = guids;
                body.Guids.Resize(2);

                Assert.Equal(2, body.Guids.Length);
                Assert.Equal(guids[0], body.Guids[0]);
                Assert.Equal(guids[1], body.Guids[1]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSPAN - IN-PLACE MODIFICATION METHODS
        // =========================================================================

        #region GhostSpan InPlace Tests

        [Fact]
        public void ReverseGuidsInPlace()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var guid3 = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2, guid3 };
                body.Guids.Reverse();

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(guid3, body.Guids[0]);
                Assert.Equal(guid2, body.Guids[1]);
                Assert.Equal(guid1, body.Guids[2]);
            }
        }

        [Fact]
        public void FillGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var fillGuid = Guid.NewGuid();
                body.Guids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
                body.Guids.Fill(fillGuid);

                Assert.Equal(3, body.Guids.Length);
                Assert.Equal(fillGuid, body.Guids[0]);
                Assert.Equal(fillGuid, body.Guids[1]);
                Assert.Equal(fillGuid, body.Guids[2]);
            }
        }

        [Fact]
        public void SortDateTimes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var date3 = new DateTime(2022, 1, 1);
                var date1 = new DateTime(2020, 1, 1);
                var date2 = new DateTime(2021, 1, 1);
                
                body.DateTimes = new[] { date3, date1, date2 };
                body.DateTimes.Sort();

                Assert.Equal(3, body.DateTimes.Length);
                Assert.Equal(date1, body.DateTimes[0]);
                Assert.Equal(date2, body.DateTimes[1]);
                Assert.Equal(date3, body.DateTimes[2]);
            }
        }

        [Fact]
        public void IndexerSetOnGuids()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var guid1 = Guid.NewGuid();
                var guid2 = Guid.NewGuid();
                var newGuid = Guid.NewGuid();
                
                body.Guids = new[] { guid1, guid2 };
                
                // Use ReplaceRange to set a single element at index 1
                body.Guids.ReplaceRange(1, 1, new[] { newGuid }.AsSpan());

                Assert.Equal(guid1, body.Guids[0]);
                Assert.Equal(newGuid, body.Guids[1]);
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF16 - SETSTRING METHODS
        // =========================================================================

        #region GhostStringUtf16 SetString Tests

        [Fact]
        public void SetStringU16FromString()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Initial";
                body.StringU16.SetString("Replaced via SetString");

                Assert.Equal("Replaced via SetString", body.StringU16.ToString());
            }
        }

        [Fact]
        public void SetStringU16FromReadOnlySpan()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Initial";
                ReadOnlySpan<char> newValue = "Span Replacement".AsSpan();
                body.StringU16.SetString(newValue);

                Assert.Equal("Span Replacement", body.StringU16.ToString());
            }
        }

        [Fact]
        public void SetStringU16FromGhostStringUtf16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Initial";
                GhostStringUtf16 source = "GhostString Source";
                body.StringU16.SetString(source);

                Assert.Equal("GhostString Source", body.StringU16.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF16 - ADDITIONAL APPEND/PREPEND TESTS
        // =========================================================================

        #region GhostStringUtf16 Append/Prepend Tests

        [Fact]
        public void AppendCharToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                body.StringU16.Append('!');

                Assert.Equal("Hello!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void AppendReadOnlySpanToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                ReadOnlySpan<char> suffix = ", World!".AsSpan();
                body.StringU16.Append(suffix);

                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void AppendGhostStringUtf16ToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello";
                GhostStringUtf16 toAppend = " World";
                body.StringU16.Append(toAppend);

                Assert.Equal("Hello World", body.StringU16.ToString());
            }
        }

        [Fact]
        public void PrependCharToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "World";
                body.StringU16.Prepend('H');

                Assert.Equal("HWorld", body.StringU16.ToString());
            }
        }

        [Fact]
        public void PrependReadOnlySpanToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "World!";
                ReadOnlySpan<char> prefix = "Hello, ".AsSpan();
                body.StringU16.Prepend(prefix);

                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void PrependGhostStringUtf16ToStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "World";
                GhostStringUtf16 toPrepend = "Hello ";
                body.StringU16.Prepend(toPrepend);

                Assert.Equal("Hello World", body.StringU16.ToString());
            }
        }

        [Fact]
        public void InsertAtReadOnlySpanInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello World";
                ReadOnlySpan<char> insert = "Beautiful ".AsSpan();
                body.StringU16.InsertAt(6, insert);

                Assert.Equal("Hello Beautiful World", body.StringU16.ToString());
            }
        }

        [Fact]
        public void InsertAtGhostStringUtf16InStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello World";
                GhostStringUtf16 insert = "Amazing ";
                body.StringU16.InsertAt(6, insert);

                Assert.Equal("Hello Amazing World", body.StringU16.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF16 - REMOVE METHODS
        // =========================================================================

        #region GhostStringUtf16 Remove Tests

        [Fact]
        public void RemoveFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World! Goodbye!";
                body.StringU16.RemoveFrom(13); // Remove from "Goodbye!"

                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveFirstCharFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                bool removed = body.StringU16.RemoveFirst('o');

                Assert.True(removed);
                Assert.Equal("Hell, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveFirstCharNotFoundInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                bool removed = body.StringU16.RemoveFirst('x');

                Assert.False(removed);
                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveFirstStringFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World! Hello, Universe!";
                bool removed = body.StringU16.RemoveFirst("Hello, ");

                Assert.True(removed);
                Assert.Equal("World! Hello, Universe!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveAllCharsFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                int removed = body.StringU16.RemoveAll('l');

                Assert.Equal(3, removed);
                Assert.Equal("Heo, Word!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void RemoveAllStringsFromStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "cat dog cat bird cat";
                int removed = body.StringU16.RemoveAll("cat");

                Assert.Equal(3, removed);
                Assert.Equal(" dog  bird ", body.StringU16.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF16 - REPLACE METHODS
        // =========================================================================

        #region GhostStringUtf16 Replace Tests

        [Fact]
        public void ReplaceRangeWithReadOnlySpanInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                ReadOnlySpan<char> replacement = "Universe".AsSpan();
                body.StringU16.ReplaceRange(7, 5, replacement);

                Assert.Equal("Hello, Universe!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceFirstInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello World Hello Universe";
                bool replaced = body.StringU16.ReplaceFirst("Hello", "Hi");

                Assert.True(replaced);
                Assert.Equal("Hi World Hello Universe", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceFirstNotFoundInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello, World!";
                bool replaced = body.StringU16.ReplaceFirst("Galaxy", "Universe");

                Assert.False(replaced);
                Assert.Equal("Hello, World!", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceAllInPlaceInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "cat dog cat bird cat";
                int count = body.StringU16.ReplaceAllInPlace("cat", "CAT");

                Assert.Equal(3, count);
                Assert.Equal("CAT dog CAT bird CAT", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceAllInPlaceWithShorterReplacementInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hello Hello Hello";
                int count = body.StringU16.ReplaceAllInPlace("Hello", "Hi");

                Assert.Equal(3, count);
                Assert.Equal("Hi Hi Hi", body.StringU16.ToString());
            }
        }

        [Fact]
        public void ReplaceAllInPlaceWithLongerReplacementInStringU16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU16 = "Hi Hi Hi";
                int count = body.StringU16.ReplaceAllInPlace("Hi", "Hello");

                Assert.Equal(3, count);
                Assert.Equal("Hello Hello Hello", body.StringU16.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - SETSTRING METHODS
        // =========================================================================

        #region GhostStringUtf8 SetString Tests

        [Fact]
        public void SetStringU8FromString()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Initial";
                body.StringU8.SetString("Replaced via SetString");

                Assert.Equal("Replaced via SetString", body.StringU8.ToString());
            }
        }

        [Fact]
        public void SetStringU8FromGhostStringUtf8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Initial";
                GhostStringUtf8 source = "GhostStringUtf8 Source";
                body.StringU8.SetString(source);

                Assert.Equal("GhostStringUtf8 Source", body.StringU8.ToString());
            }
        }

        [Fact]
        public void SetStringU8FromGhostStringUtf16()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Initial";
                GhostStringUtf16 source = "UTF16 Source";
                body.StringU8.SetString(source);

                Assert.Equal("UTF16 Source", body.StringU8.ToString());
            }
        }

        [Fact]
        public void SetBytesU8FromReadOnlySpan()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Initial";
                var bytes = System.Text.Encoding.UTF8.GetBytes("Bytes Source");
                body.StringU8.SetBytes(bytes.AsSpan());

                Assert.Equal("Bytes Source", body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - APPEND/PREPEND TESTS
        // =========================================================================

        #region GhostStringUtf8 Append/Prepend Tests

        [Fact]
        public void AppendReadOnlySpanBytesToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";
                var suffix = System.Text.Encoding.UTF8.GetBytes(", World!");
                body.StringU8.Append(suffix.AsSpan());

                Assert.Equal("Hello, World!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void AppendGhostStringUtf8ToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";
                GhostStringUtf8 toAppend = " World";
                body.StringU8.Append(toAppend);

                Assert.Equal("Hello World", body.StringU8.ToString());
            }
        }

        [Fact]
        public void AppendGhostStringUtf16ToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";
                GhostStringUtf16 toAppend = " UTF16";
                body.StringU8.Append(toAppend);

                Assert.Equal("Hello UTF16", body.StringU8.ToString());
            }
        }

        [Fact]
        public void PrependReadOnlySpanBytesToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "World!";
                var prefix = System.Text.Encoding.UTF8.GetBytes("Hello, ");
                body.StringU8.Prepend(prefix.AsSpan());

                Assert.Equal("Hello, World!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void PrependGhostStringUtf8ToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "World";
                GhostStringUtf8 toPrepend = "Hello ";
                body.StringU8.Prepend(toPrepend);

                Assert.Equal("Hello World", body.StringU8.ToString());
            }
        }

        [Fact]
        public void PrependGhostStringUtf16ToStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "World";
                GhostStringUtf16 toPrepend = "UTF16 ";
                body.StringU8.Prepend(toPrepend);

                Assert.Equal("UTF16 World", body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - REMOVE METHODS
        // =========================================================================

        #region GhostStringUtf8 Remove Tests

        [Fact]
        public void RemoveBytesAtStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World!";
                body.StringU8.RemoveBytesAt(5, 7); // Remove ", World"

                Assert.Equal("Hello!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void RemoveBytesFromStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World! Goodbye!";
                body.StringU8.RemoveBytesFrom(13);

                Assert.Equal("Hello, World!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void RemoveFirstBytesFromStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello World Hello Universe";
                var searchBytes = System.Text.Encoding.UTF8.GetBytes("Hello ");
                bool removed = body.StringU8.RemoveFirstBytes(searchBytes.AsSpan());

                Assert.True(removed);
                Assert.Equal("World Hello Universe", body.StringU8.ToString());
            }
        }

        [Fact]
        public void RemoveAllBytesFromStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "cat dog cat bird cat";
                var searchBytes = System.Text.Encoding.UTF8.GetBytes("cat");
                int removed = body.StringU8.RemoveAllBytes(searchBytes.AsSpan());

                Assert.Equal(3, removed);
                Assert.Equal(" dog  bird ", body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - REPLACE METHODS
        // =========================================================================

        #region GhostStringUtf8 Replace Tests

        [Fact]
        public void ReplaceBytesRangeWithBytesSpanInStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World!";
                var replacement = System.Text.Encoding.UTF8.GetBytes("Universe");
                body.StringU8.ReplaceBytesRange(7, 5, replacement.AsSpan());

                Assert.Equal("Hello, Universe!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ReplaceBytesRangeWithStringInStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello, World!";
                body.StringU8.ReplaceBytesRange(7, 5, "Galaxy");

                Assert.Equal("Hello, Galaxy!", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ReplaceFirstBytesInStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "cat dog cat bird";
                var oldValue = System.Text.Encoding.UTF8.GetBytes("cat");
                var newValue = System.Text.Encoding.UTF8.GetBytes("CAT");
                bool replaced = body.StringU8.ReplaceFirstBytes(oldValue.AsSpan(), newValue.AsSpan());

                Assert.True(replaced);
                Assert.Equal("CAT dog cat bird", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ReplaceAllBytesInPlaceInStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "cat dog cat bird cat";
                var oldValue = System.Text.Encoding.UTF8.GetBytes("cat");
                var newValue = System.Text.Encoding.UTF8.GetBytes("CAT");
                int count = body.StringU8.ReplaceAllBytesInPlace(oldValue.AsSpan(), newValue.AsSpan());

                Assert.Equal(3, count);
                Assert.Equal("CAT dog CAT bird CAT", body.StringU8.ToString());
            }
        }

        #endregion

        // =========================================================================
        // GHOSTSTRINGUTF8 - INPLACE METHODS
        // =========================================================================

        #region GhostStringUtf8 InPlace Tests

        [Fact]
        public void TrimAsciiInPlaceStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "   Hello World   ";
                body.StringU8.TrimAsciiInPlace();

                Assert.Equal("Hello World", body.StringU8.ToString());
            }
        }

        [Fact]
        public void TrimAsciiStartInPlaceStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "   Hello World   ";
                body.StringU8.TrimAsciiStartInPlace();

                Assert.Equal("Hello World   ", body.StringU8.ToString());
            }
        }

        [Fact]
        public void TrimAsciiEndInPlaceStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "   Hello World   ";
                body.StringU8.TrimAsciiEndInPlace();

                Assert.Equal("   Hello World", body.StringU8.ToString());
            }
        }

        [Fact]
        public void ReverseBytesInPlaceStringU8()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.StringU8 = "Hello";
                body.StringU8.ReverseBytesInPlace();

                Assert.Equal("olleH", body.StringU8.ToString());
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

                for (int i = 0; i < 1000; i++)
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
                    new DateTime(2021, 6, 15),
                    new DateTime(2022, 12, 31)
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

        [Fact]
        public void RemoveRangeFromDateTimesArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                var dates = Enumerable.Range(0, 5).Select(i => DateTime.Now.AddDays(i)).ToArray();
                body.DateTimes = dates;

                body.DateTimes.RemoveRange(1, 3);
                Assert.Equal(2, body.DateTimes.Length);
                Assert.Equal(dates[0], body.DateTimes[0]);
                Assert.Equal(dates[4], body.DateTimes[1]);
            }
        }

        [Fact]
        public void ClearDateTimesArray()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansSmall();

                body.DateTimes = new[] { DateTime.Now, DateTime.UtcNow };
                Assert.Equal(2, body.DateTimes.Length);

                body.DateTimes.Clear();
                Assert.True(body.DateTimes.IsEmpty);
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
    }
}
