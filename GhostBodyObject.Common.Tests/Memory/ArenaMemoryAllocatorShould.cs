using GhostBodyObject.Common.Memory;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace GhostBodyObject.Common.Tests.Memory;

public class TransientGhostMemoryAllocatorShould
{
    // Helper to get the underlying array identity to check for reallocations
    private static byte[]? GetUnderlyingArray(Memory<byte> memory)
    {
        if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment))
        {
            return segment.Array;
        }
        return null;
    }

    [Fact]
    public void Return_Empty_Memory_When_Allocating_Zero_Size()
    {
        var memory = TransientGhostMemoryAllocator.Allocate(0);
        Assert.True(memory.IsEmpty);
    }

    [Fact]
    public void Throw_ArgumentOutOfRangeException_When_Allocating_Negative_Size()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TransientGhostMemoryAllocator.Allocate(-1));
    }

    [Fact]
    public void Use_Arena_Strategy_For_Small_Blocks()
    {
        // Allocate 100 bytes (Should fit in Arena)
        var mem = TransientGhostMemoryAllocator.Allocate(100);

        Assert.Equal(100, mem.Length);

        // In the Arena strategy, the underlying array is the large shared page (64KB default).
        // We verify that we got a slice of a larger array.
        bool hasArray = MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> segment);
        Assert.True(hasArray);
        Assert.NotNull(segment.Array);
        Assert.True(segment.Array!.Length > 100);
    }

    [Fact]
    public void Use_Dedicated_Array_For_Large_Blocks()
    {
        // Threshold is 32KB. Let's allocate 50KB.
        int size = 50 * 1024;
        var mem = TransientGhostMemoryAllocator.Allocate(size);

        Assert.Equal(size, mem.Length);

        // For large blocks, it allocates a dedicated array.
        bool hasArray = MemoryMarshal.TryGetArray(mem, out ArraySegment<byte> segment);
        Assert.True(hasArray);
        Assert.NotNull(segment.Array);

        // Verify it is a dedicated array (length close to requested size)
        Assert.True(segment.Array!.Length >= size);
    }

    [Fact]
    public void Not_Reallocate_When_Growing_Within_OverAllocation_Limits()
    {
        // 1. Allocate 100 bytes.
        // ChunkSizeComputation: SizeToIndex(100) -> likely maps to 128 bytes physical capacity.
        var mem = TransientGhostMemoryAllocator.Allocate(100);
        var originalArray = GetUnderlyingArray(mem);

        // Write some data to verify integrity
        mem.Span[0] = 0xAA;

        // 2. Resize to 110 bytes (Should fit in the 128 byte physical gap).
        TransientGhostMemoryAllocator.Resize(ref mem, 110);

        // Assertions
        Assert.Equal(110, mem.Length);
        Assert.Equal(0xAA, mem.Span[0]); // Data preserved

        // CRITICAL: The underlying array object should be exactly the same
        Assert.Same(originalArray, GetUnderlyingArray(mem));
    }

    [Fact]
    public void Reallocate_When_Growing_Beyond_Capacity()
    {
        // 1. Allocate 100 bytes. (Physical ~128)
        var mem = TransientGhostMemoryAllocator.Allocate(100);
        var originalArray = GetUnderlyingArray(mem);
        mem.Span[0] = 0xBB;

        // 2. Resize to 200 bytes (Exceeds physical 128).
        TransientGhostMemoryAllocator.Resize(ref mem, 200);

        // Assertions
        Assert.Equal(200, mem.Length);
        Assert.Equal(0xBB, mem.Span[0]);

        // Simpler check: ensure we can write to the new end without crashing
        mem.Span[199] = 0xFF;
    }

    [Fact]
    public void Not_Reallocate_When_Minor_Shrink_On_Dedicated_Array()
    {
        // 1. Allocate 100KB (Dedicated Array)
        int size = 100 * 1024;
        var mem = TransientGhostMemoryAllocator.Allocate(size);
        var originalArray = GetUnderlyingArray(mem);

        // 2. Resize to 90KB (90KB > 50% of 100KB, should NOT shrink)
        TransientGhostMemoryAllocator.Resize(ref mem, 90 * 1024);

        // Assertions
        Assert.Equal(90 * 1024, mem.Length);
        Assert.Same(originalArray, GetUnderlyingArray(mem));
    }

    [Fact]
    public void Reallocate_When_Major_Shrink_On_Dedicated_Array()
    {
        // 1. Allocate 2MB
        int size = 2 * 1024 * 1024;
        var mem = TransientGhostMemoryAllocator.Allocate(size);
        var originalArray = GetUnderlyingArray(mem);

        // 2. Resize to 1KB (Massive shrink -> Should trigger reallocation)
        TransientGhostMemoryAllocator.Resize(ref mem, 1024);

        // Assertions
        Assert.Equal(1024, mem.Length);
        var newArray = GetUnderlyingArray(mem);

        // The allocator should have discarded the 2MB array and allocated a small one
        Assert.NotSame(originalArray, newArray);
        Assert.True(newArray!.Length < size); // The backing store should be smaller now
    }

    [Fact]
    public void Prevent_Salami_Slicing_Drift()
    {
        // This tests the logic: "if (actualArrayLength > DefaultPageSize)"

        // 1. Start with 1MB (Large Block)
        var mem = TransientGhostMemoryAllocator.Allocate(1024 * 1024);
        var originalArray = GetUnderlyingArray(mem);

        // 2. Shrink to 600KB ( > 50%, Keep Array)
        TransientGhostMemoryAllocator.Resize(ref mem, 600 * 1024);
        Assert.Same(originalArray, GetUnderlyingArray(mem));

        // 3. Shrink to 400KB.
        // IF the logic checked against 600KB, 400KB > 50% of 600KB, it would keep it.
        // BUT the logic should check against the REAL 1MB capacity.
        // 400KB < 50% of 1MB. It SHOULD shrink.
        TransientGhostMemoryAllocator.Resize(ref mem, 400 * 1024);

        // Assertions
        var newArray = GetUnderlyingArray(mem);
        Assert.NotSame(originalArray, newArray);
    }

    [Fact]
    public void Reallocate_When_Drastic_Shrink_In_Arena()
    {
        // 1. Allocate 1024 bytes (Arena)
        var mem = TransientGhostMemoryAllocator.Allocate(1024);

        // 2. Resize to 64 bytes.
        // 64 < (1024 / 2). Should Reallocate.
        TransientGhostMemoryAllocator.Resize(ref mem, 64);

        Assert.Equal(64, mem.Length);

        // Verify logical size is strictly 64, accessing beyond throws
        Assert.Throws<IndexOutOfRangeException>(() => { var b = mem.Span[100]; });
    }

    [Fact]
    public void Copy_Data_Correctly_During_Resize()
    {
        var mem = TransientGhostMemoryAllocator.Allocate(100);
        for (int i = 0; i < 100; i++)
        {
            mem.Span[i] = (byte)i;
        }

        // Trigger a resize that forces reallocation (Growth)
        TransientGhostMemoryAllocator.Resize(ref mem, 200);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal((byte)i, mem.Span[i]);
        }

        // Trigger a resize that forces reallocation (Shrink)
        TransientGhostMemoryAllocator.Resize(ref mem, 50);
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal((byte)i, mem.Span[i]);
        }
    }
}