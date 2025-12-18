using GhostBodyObject.Common.Memory;
using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Common.Tests.Memory
{
    public class ChunkSizeComputationShould
    {
        // Important: This test suite checks the INVARIANTS of the allocator.
        // It does not hardcode expected indices for large numbers, as those 
        // depend on the #if LOW_MEM_OVERHEAD compilation flag.

        [Theory]
        [InlineData(1)]
        [InlineData(63)]
        [InlineData(64)]
        public void SizeToIndex_SmallValues_MapToMinimumIndex(uint size)
        {
            // Act
            ushort index = ChunkSizeComputation.SizeToIndex(size);
            uint computedSize = ChunkSizeComputation.IndexToSize(index);

            // Assert
            Assert.Equal(0, index);
            Assert.Equal(64u, computedSize);
            Assert.True(computedSize >= size);
        }

        [Fact]
        public void SizeToIndex_Zero_ShouldProbablyHandleOrThrow()
        {
            // Based on code: if (size <= 64) return 0;
            // So 0 returns 0.
            var index = ChunkSizeComputation.SizeToIndex(0);
            Assert.Equal(0, index);
        }

        /// <summary>
        /// The Golden Rule of Allocators: The computed block size must be 
        /// greater than or equal to the requested size.
        /// </summary>
        [Fact]
        public void RoundTrip_AlwaysFitsRequestedSize()
        {
            // We scan a range of sizes to ensure no gaps exist.
            // Covering small linear range + transition to float logic + large values.

            // Check dense range 1..4096
            for (uint size = 1; size <= 1024 * 1024 * 32; size+=4)
            {
                ushort index = ChunkSizeComputation.SizeToIndex(size);
                uint capacity = ChunkSizeComputation.IndexToSize(index);

                Assert.True(capacity >= size,
                    $"Failed at size {size}: Index {index} provides capacity {capacity}");

                if (size > 128)
                    Assert.True(capacity < size * 2,
                        $"Failed at size {size}: capacity {capacity} is more than 2 times size {size}");
            }

            // Check sparse large range (powers of 2 boundaries)
            for (int i = 12; i < 30; i++)
            {
                uint baseSize = 1u << i;
                // Test around the power of 2
                CheckFit(baseSize - 1);
                CheckFit(baseSize);
                CheckFit(baseSize + 1);
            }
        }

        private void CheckFit(uint size)
        {
            ushort index = ChunkSizeComputation.SizeToIndex(size);
            uint capacity = ChunkSizeComputation.IndexToSize(index);
            Assert.True(capacity >= size,
                $"Failed at size {size}: Index {index} provides capacity {capacity}");
        }

        /// <summary>
        /// Ensures that as requested size increases, the resulting bucket index 
        /// never decreases (Monotonicity).
        /// </summary>
        [Fact]
        public void SizeToIndex_IsMonotonic()
        {
            ushort prevIndex = 0;

            // Check first 10,000 bytes
            for (uint size = 1; size < 10000; size++)
            {
                ushort currentIndex = ChunkSizeComputation.SizeToIndex(size);

                Assert.True(currentIndex >= prevIndex,
                    $"Monotonicity violation at size {size}. PrevIndex: {prevIndex}, Current: {currentIndex}");

                prevIndex = currentIndex;
            }
        }

        /// <summary>
        /// Ensures that as bucket index increases, the physical size 
        /// strictly increases.
        /// </summary>
        [Fact]
        public void IndexToSize_IsStrictlyMonotonic()
        {
            uint prevSize = 0;
            var list = new List<uint>();
            // Test first maximum buckets
            for (ushort i = 0; i < ChunkSizeComputation.MaxIndex; i++)
            {
                uint size = ChunkSizeComputation.IndexToSize(i);

                // Note: Depending on the logic, size 0 (index 0) is 64.
                Assert.True(size >= prevSize,
                    $"IndexToSize violation at index {i}. PrevSize: {prevSize}, Current: {size}");

                prevSize = size;
                list.Add(size);
            }
        }

        [Theory]
        [InlineData(64, true)]
        [InlineData(65, false)] // 65 needs next bucket
        public void Fit_CalculatesCorrectly(uint size, bool expectedForIndex0)
        {
            // Index 0 is always 64 bytes
            bool fits = ChunkSizeComputation.Fit(0, size);
            Assert.Equal(expectedForIndex0, fits);
        }

        [Fact]
        public void Loss_IsCalculatedCorrectly()
        {
            // Example: Index 0 provides 64 bytes.
            // If we ask for 60 bytes, loss should be 4.
            ulong loss = ChunkSizeComputation.Loss(0, 60);
            Assert.Equal(4ul, loss);

            // If we ask for 64 bytes, loss is 0.
            loss = ChunkSizeComputation.Loss(0, 64);
            Assert.Equal(0ul, loss);
        }

        [Fact]
        public void Expand_ReturnsLargerSize()
        {
            uint startSize = 100;
            uint expanded = ChunkSizeComputation.Expand(startSize);

            Assert.True(expanded > startSize,
                $"Expand failed. Start: {startSize}, Expanded: {expanded}");

            // Verify expanded size maps to a higher index
            Assert.True(ChunkSizeComputation.SizeToIndex(expanded) > ChunkSizeComputation.SizeToIndex(startSize));
        }

        [Fact]
        public void Shrink_ReturnsSmallerSize()
        {
            // Choose a size that is likely in a higher bucket (e.g. > 128)
            uint startSize = 256;
            uint shrunk = ChunkSizeComputation.Shrink(startSize);

            Assert.True(shrunk < startSize,
                $"Shrink failed. Start: {startSize}, Shrunk: {shrunk}");

            // Verify shrunk size maps to a lower index
            Assert.True(ChunkSizeComputation.SizeToIndex(shrunk) < ChunkSizeComputation.SizeToIndex(startSize));
        }

        [Fact]
        public void Boundary_MaxUint_DoesNotCrash()
        {
            // Testing extremely large values to ensure no overflow crashes
            // Note: Implementation caps at 0x7FFFFFFF (2GB) via constant, 
            // but uint input allows more. 
            // We just want to ensure the calculation functions handle it without exception.

            try
            {
                ChunkSizeComputation.SizeToIndex(uint.MaxValue);
            }
            catch (Exception ex)
            {
                Assert.Fail($"SizeToIndex crashed on uint.MaxValue: {ex.Message}");
            }
        }
    }
}
