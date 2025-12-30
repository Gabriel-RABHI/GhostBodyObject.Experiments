using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Runtime.InteropServices;
using Xunit;

// Adjust namespace to match your test project structure
namespace GhostBodyObject.Common.Tests.Objects
{
    public class GhostIdShould
    {
        // ---------------------------------------------------------
        // 1. Structural & Layout Tests
        // ---------------------------------------------------------

        [Fact]
        public unsafe void Have_Explicit_Size_Of_16_Bytes()
        {
            Assert.Equal(16, sizeof(GhostId));
        }

        // ---------------------------------------------------------
        // 2. Construction & Packing Tests
        // ---------------------------------------------------------

        [Fact]
        public void Pack_And_Unpack_Fields_Correctly()
        {
            // Arrange
            var kind = (GhostIdKind)5; // Arbitrary valid value (0-7)
            ushort typeId = 1234;      // Arbitrary valid value (0-8191)
            ulong random = 0xCAFEBABE_DEADBEEF;

            // Calculate a valid timestamp relative to the Epoch (2025-01-01)
            // Let's pick a time 100 seconds after epoch
            var epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var targetTime = epoch.AddSeconds(100);

            // Convert targetTime back to the internal microsecond format expected by the constructor
            ulong timestampUs = (ulong)((targetTime.Ticks - epoch.Ticks) / 10);

            // Act
            var id = new GhostId(kind, typeId, timestampUs, random);

            // Assert
            Assert.Equal(kind, id.Kind);
            Assert.Equal(typeId, id.TypeIdentifier);
            Assert.Equal(targetTime, id.CreatedAt);
        }

        [Fact]
        public void Mask_Overflowing_TypeIdentifier()
        {
            // The TypeIdentifier is 13 bits (Max 8191 / 0x1FFF)
            // We pass 8193 (0x2001), which has the 14th bit set. 
            // Expectation: 0x2001 & 0x1FFF = 0x0001
            ushort overflowingType = 0x2001;

            var id = new GhostId((GhostIdKind)1, overflowingType, 0, 0);

            Assert.Equal(1, id.TypeIdentifier);
        }

        [Fact]
        public void Mask_Overflowing_Kind()
        {
            // The Kind is 3 bits (Max 7)
            // We pass value 9 (1001 binary).
            // Expectation: 1001 & 0111 = 0001 (1)
            var overflowingKind = (GhostIdKind)9;

            var id = new GhostId(overflowingKind, 1, 0, 0);

            Assert.Equal((GhostIdKind)1, id.Kind);
        }

        // ---------------------------------------------------------
        // 3. NewId Generation Tests
        // ---------------------------------------------------------

        [Fact]
        public void Generate_New_Id_With_Current_Time()
        {
            var start = DateTime.UtcNow;
            var id = GhostId.NewId((GhostIdKind)2, 500);
            var end = DateTime.UtcNow;

            // Assert Metadata
            Assert.Equal((GhostIdKind)2, id.Kind);
            Assert.Equal(500, id.TypeIdentifier);

            // Assert Time (Allow small delta for execution time)
            // Note: Precision loss is expected due to microseconds truncation
            var delta = (id.CreatedAt - start).TotalMilliseconds;

            // It should be very close to 'now', but since the internal epoch is 2025, 
            // ensure your system clock is >= 2025-01-01 for this test to make sense strictly,
            // or simply ensure the delta is reasonable relative to the generated time.
            Assert.InRange(id.CreatedAt, start.AddMilliseconds(-1), end.AddMilliseconds(1));
        }

        [Fact]
        public void Generate_Unique_Ids_Sequentially()
        {
            // Even if called in tight loop, Random or Time should likely differ.
            // Note: If XorShift seeds identitically or time is too fast, this could flake,
            // but functionally distinct randoms are expected.
            var id1 = GhostId.NewId((GhostIdKind)1, 100);
            var id2 = GhostId.NewId((GhostIdKind)1, 100);

            Assert.NotEqual(id1, id2);
        }

        // ---------------------------------------------------------
        // 4. Equality Tests
        // ---------------------------------------------------------

        [Fact]
        public void Be_Equal_When_Fields_Are_Identical()
        {
            var id1 = new GhostId((GhostIdKind)1, 10, 1000, 500);
            var id2 = new GhostId((GhostIdKind)1, 10, 1000, 500);

            Assert.True(id1.Equals(id2));
            Assert.True(id1.Equals((object)id2));
            Assert.True(id1 == id2);
            Assert.False(id1 != id2);
            Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
        }

        [Fact]
        public void Not_Be_Equal_When_Fields_Differ()
        {
            var baseId = new GhostId((GhostIdKind)1, 10, 1000, 500);

            var diffKind = new GhostId((GhostIdKind)2, 10, 1000, 500);
            var diffType = new GhostId((GhostIdKind)1, 11, 1000, 500);
            var diffTime = new GhostId((GhostIdKind)1, 10, 1001, 500);
            var diffRand = new GhostId((GhostIdKind)1, 10, 1000, 501);

            Assert.NotEqual(baseId, diffKind);
            Assert.NotEqual(baseId, diffType);
            Assert.NotEqual(baseId, diffTime);
            Assert.NotEqual(baseId, diffRand);

            Assert.True(baseId != diffKind);
            Assert.False(baseId == diffKind);
        }

        // ---------------------------------------------------------
        // 5. Comparison / Sorting Tests
        // ---------------------------------------------------------

        [Fact]
        public void Sort_By_Kind_First()
        {
            // Kind 1 vs Kind 2
            // Header layout: Kind is at bits 61-63 (Highest order)
            var id1 = new GhostId((GhostIdKind)1, 100, 1000, 0);
            var id2 = new GhostId((GhostIdKind)2, 100, 1000, 0);

            // id1 should be less than id2
            Assert.True(id1.CompareTo(id2) < 0);
        }

        [Fact]
        public void Sort_By_Type_Second()
        {
            // Same Kind, Different Type
            // Type is bits 48-60
            var id1 = new GhostId((GhostIdKind)1, 10, 1000, 0);
            var id2 = new GhostId((GhostIdKind)1, 20, 1000, 0);

            Assert.True(id1.CompareTo(id2) < 0);
        }

        [Fact]
        public void Sort_By_Timestamp_Third()
        {
            // Same Kind, Same Type, Different Time
            var id1 = new GhostId((GhostIdKind)1, 10, 1000, 0);
            var id2 = new GhostId((GhostIdKind)1, 10, 2000, 0);

            Assert.True(id1.CompareTo(id2) < 0);
        }

        [Fact]
        public void Sort_By_Random_Last()
        {
            // Identical Header, Different Random
            var id1 = new GhostId((GhostIdKind)1, 10, 1000, 100);
            var id2 = new GhostId((GhostIdKind)1, 10, 1000, 200);

            Assert.True(id1.CompareTo(id2) < 0);
        }

        // ---------------------------------------------------------
        // 6. Formatting Tests
        // ---------------------------------------------------------

        [Fact]
        public void Format_ToString_Correctly()
        {
            var kind = (GhostIdKind)3;
            ushort type = 255;
            ulong time = 0xABC;
            ulong rand = 0xDEF;

            var id = new GhostId(kind, type, time, rand);

            // Expected format: $"{Kind}-{TypeIdentifier}-{_header & TimestampMask:X}-{_random:X}"
            // Note: The third part is Hex of Timestamp
            string expected = $"{kind}-{type}-{time:X}-{rand:X}";

            Assert.Equal(expected, id.ToString());
        }
    }
}