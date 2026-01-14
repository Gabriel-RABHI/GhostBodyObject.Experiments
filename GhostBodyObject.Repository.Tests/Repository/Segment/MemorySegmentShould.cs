using GhostBodyObject.Common.Memory;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Segment;
using System;
using System.IO;
using Xunit;

namespace GhostBodyObject.Repository.Tests.Repository.Segment
{
    public unsafe class MemorySegmentShould
    {
        [Fact]
        public void CreateInMemory()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0);
            Assert.NotNull(segment);
            Assert.Equal(SegmentImplementationType.LOHPinnedMemory, segment.SegmentType);
            Assert.True(segment.Deletable);
            Assert.True(segment.BasePointer != null);
            Assert.Equal(1024 * 1024 * 8, segment.FreeSpace); // Default capacity
        }

        [Fact]
        public void CreateInMemoryWithCustomCapacity()
        {
            int capacity = 2048;
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, capacity);
            Assert.NotNull(segment);
            Assert.Equal(capacity, segment.FreeSpace);
        }

        [Fact]
        public void ThrowWhenCapacityIsTooSmall()
        {
            Assert.Throws<InvalidOperationException>(() => MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 100));
        }

        [Fact]
        public void AllocateMemory()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 2048);
            int allocSize = 128;
            
            PinnedMemory<byte> memory = segment.Allocate(allocSize);
            
            Assert.Equal(allocSize, memory.Length);
            Assert.True(memory.Ptr != null);
            
            // Check if offset advanced
            Assert.Equal(2048 - allocSize, segment.FreeSpace);

            // Verify memory is within segment bounds
            Assert.True(memory.Ptr >= segment.BasePointer);
            Assert.True(memory.Ptr < segment.BasePointer + 2048);
        }

        [Fact]
        public void WriteUnmanagedData()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 2048);
            int valueToWrite = 0x12345678;
            
            int offset = segment.Write(valueToWrite);
            
            Assert.Equal(0, offset); // First write should be at offset 0
            Assert.Equal(2048 - sizeof(int), segment.FreeSpace);
            
            // Verify data integrity
            int* pValue = (int*)(segment.BasePointer + offset);
            Assert.Equal(valueToWrite, *pValue);
        }

        [Fact]
        public void PerformMultipleAllocationsAndWrites()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 4096);
            
            // 1. Allocate block
            var mem1 = segment.Allocate(100);
            Assert.Equal(0, (int)(mem1.Ptr - segment.BasePointer));
            
            // 2. Write int
            int val1 = 42;
            int offset1 = segment.Write(val1);
            Assert.Equal(100, offset1);
            
            // 3. Allocate another block
            var mem2 = segment.Allocate(50);
            Assert.Equal(100 + sizeof(int), (int)(mem2.Ptr - segment.BasePointer));
            
            // 4. Write long
            long val2 = 999999L;
            int offset2 = segment.Write(val2);
            Assert.Equal(100 + sizeof(int) + 50, offset2);

            // Check integrity
            Assert.Equal(val1, *(int*)(segment.BasePointer + offset1));
            Assert.Equal(val2, *(long*)(segment.BasePointer + offset2));
            
            // Check total usage
            int expectedUsed = 100 + sizeof(int) + 50 + sizeof(long);
            Assert.Equal(4096 - expectedUsed, segment.FreeSpace);
        }

        [Fact]
        public void ThrowOverflowExceptionOnAllocate()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 2048);
            
            // Allocate almost everything
            segment.Allocate(2000);
            
            // Try to allocate more than remaining
            Assert.Throws<OverflowException>(() => segment.Allocate(100));
        }

        [Fact]
        public void ThrowOverflowExceptionOnWrite()
        {
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0, 2048);
            
            // Allocate almost everything so sizeof(int) won't fit
            segment.Allocate(2046); // 2 bytes left
            
            Assert.Throws<OverflowException>(() => segment.Write(123));
        }

        [Fact]
        public void EnsureBasePointerIsPinned()
        {
            // This test is a bit theoretical as we trust GC.AllocateUninitializedArray(pinned: true)
            // but we can verify we got a pointer.
            var segment = MemorySegment.NewInMemory(SegmentStoreMode.InMemoryVolatileRepository, 0);
            Assert.True(segment.BasePointer != null);
            
            // Do a write through the pointer to ensure it's valid access
            *segment.BasePointer = 0xFF;
            Assert.Equal(0xFF, *segment.BasePointer);
        }
    }
}
