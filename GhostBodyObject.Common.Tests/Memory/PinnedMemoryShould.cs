using System.Runtime.CompilerServices;

namespace GhostBodyObject.Common.Tests.Memory;

public unsafe class PinnedMemoryShould
{
    // -------------------------------------------------------------------------
    // TEST HELPERS
    // -------------------------------------------------------------------------

    struct Vector3
    {
        public float X, Y, Z;
    }

    struct MixedStruct
    {
        public byte Header;
        public int Id;
        public double Value;
    }

    // -------------------------------------------------------------------------
    // CONSTRUCTOR & PROPERTY TESTS
    // -------------------------------------------------------------------------

    [Fact]
    public void PinnedMemoryShould_InitializeCorrectly_FromByteArray()
    {
        // Arrange
        byte[] buffer = new byte[100];
        // Initialize with some data to verify pointer access
        buffer[10] = 0xFF;

        // Act
        // Pinning range: index 10, length 20
        var mem = new PinnedMemory<byte>(buffer, 10, 20);

        // Assert
        Assert.Equal(20, mem.Length);
        Assert.False(mem.IsEmpty);
        Assert.Equal(buffer, mem.MemoryOwner);

        // Verify pointer arithmetic
        // The first byte of 'mem' should be buffer[10]
        Assert.Equal(0xFF, *mem.Ptr);
    }

    [Fact]
    public void InitializeCorrectly_FromGenericArray()
    {
        // Arrange
        int[] buffer = { 10, 20, 30, 40, 50 };

        // Act
        // Pinning range: index 2 (value 30), length 2
        var mem = new PinnedMemory<int>(buffer, 2, 2);

        // Assert
        Assert.Equal(2, mem.Length);
        Assert.Equal(30, *mem.Ptr);
        Assert.Equal(40, *(mem.Ptr + 1));
    }

    [Fact]
    public void InitializeCorrectly_FromRawPointer()
    {
        // Arrange
        int value = 12345;
        int* ptr = &value;

        // Act
        var mem = new PinnedMemory<int>(null, ptr, 1);

        // Assert
        Assert.Equal(1, mem.Length);
        Assert.Null(mem.MemoryOwner);
        Assert.Equal(12345, *mem.Ptr);
    }

    // -------------------------------------------------------------------------
    // INDEXER TESTS
    // -------------------------------------------------------------------------

    [Fact]
    public void ReadWrite_ViaIndexer()
    {
        // Arrange
        byte[] buffer = new byte[10];
        var mem = new PinnedMemory<byte>(buffer, 0, 10);

        // Act
        mem[0] = 10;
        mem[5] = 50;

        // Assert
        Assert.Equal(10, buffer[0]);
        Assert.Equal(50, buffer[5]);
        Assert.Equal(10, mem[0]);
    }

    // -------------------------------------------------------------------------
    // GENERIC GET / SET TESTS (Byte Offset Logic)
    // -------------------------------------------------------------------------

    // Note: Set<T> / Get<T> logic depends on the pointer type of the struct.
    // If PinnedMemory<byte>, offsets are bytes.

    [Fact]
    public void SetAndGet_Primitives_AtByteOffsets()
    {
        // Arrange
        byte[] buffer = new byte[16];
        var mem = new PinnedMemory<byte>(buffer, 0, 16);
        double value = 123.456;

        // Act
        // Write a double (8 bytes) at offset 4
        mem.Set(4, value);

        // Read it back
        double result = mem.Get<double>(4);

        // Assert
        Assert.Equal(value, result);

        // Verify underlying array bytes were modified
        double fromArray = BitConverter.ToDouble(buffer, 4);
        Assert.Equal(value, fromArray);
    }

    [Fact]
    public void SetAndGet_Structs_AtByteOffsets()
    {
        // Arrange
        byte[] buffer = new byte[64];
        var mem = new PinnedMemory<byte>(buffer, 0, 64);
        var expected = new Vector3 { X = 1.0f, Y = 2.0f, Z = 3.0f };

        // Act
        mem.Set(10, expected);
        var result = mem.Get<Vector3>(10);

        // Assert
        Assert.Equal(expected.X, result.X);
        Assert.Equal(expected.Y, result.Y);
        Assert.Equal(expected.Z, result.Z);
    }

    [Fact]
    public void HandleUnalignedAccess()
    {
        // Arrange
        byte[] buffer = new byte[16];
        var mem = new PinnedMemory<byte>(buffer, 0, 16);
        long val = 99999999999;

        // Act
        // Write long (8 bytes) at odd offset 1
        mem.Set(1, val);
        long result = mem.Get<long>(1);

        // Assert
        Assert.Equal(val, result);
        Assert.Equal(0, buffer[0]); // Ensure no overwrite of previous byte
    }

    // -------------------------------------------------------------------------
    // BULK OPERATIONS (WriteArray / ReadArray)
    // -------------------------------------------------------------------------

    [Fact]
    public void WriteArray_StructsToBytes()
    {
        // Arrange
        byte[] buffer = new byte[100];
        var mem = new PinnedMemory<byte>(buffer, 0, 100);

        var source = new Vector3[]
        {
                new Vector3 { X=1, Y=1, Z=1 },
                new Vector3 { X=2, Y=2, Z=2 }
        };

        // Act
        // Write array starting at byte offset 20 of the memory
        mem.WriteArray(20, source);

        // Assert
        // Manually check array content
        // Vector3 is 12 bytes. 
        // First vector at 20
        Assert.Equal(1, BitConverter.ToSingle(buffer, 20)); // X
        Assert.Equal(1, BitConverter.ToSingle(buffer, 24)); // Y
        Assert.Equal(1, BitConverter.ToSingle(buffer, 28)); // Z

        // Second vector at 32 (20 + 12)
        Assert.Equal(2, BitConverter.ToSingle(buffer, 32)); // X
    }

    [Fact]
    public void ReadArray_BytesToStructs()
    {
        // Arrange
        byte[] buffer = new byte[100];
        var mem = new PinnedMemory<byte>(buffer, 0, 100);

        // Populate buffer manually
        var v1 = new Vector3 { X = 10, Y = 20, Z = 30 };
        Unsafe.WriteUnaligned(ref buffer[10], v1);

        var dest = new Vector3[1];

        // Act
        // Read 1 struct from offset 10
        mem.ReadArray(10, dest);

        // Assert
        Assert.Equal(10, dest[0].X);
        Assert.Equal(20, dest[0].Y);
        Assert.Equal(30, dest[0].Z);
    }

    // -------------------------------------------------------------------------
    // SPAN & SLICING TESTS
    // -------------------------------------------------------------------------

    [Fact]
    public void ExposeAsSpan()
    {
        // Arrange
        byte[] buffer = { 1, 2, 3, 4, 5 };
        var mem = new PinnedMemory<byte>(buffer, 0, 5);

        // Act
        Span<byte> span = mem.Span;

        // Assert
        Assert.Equal(5, span.Length);
        Assert.Equal(3, span[2]);

        // Verify modification via Span affects memory
        span[0] = 99;
        Assert.Equal(99, buffer[0]);
    }

    [Fact]
    public void _Slice_Correctly()
    {
        // Arrange
        byte[] buffer = { 0, 10, 20, 30, 40, 50, 60 };
        var mem = new PinnedMemory<byte>(buffer, 0, buffer.Length);

        // Act - Slice starting at index 2 (value 20)
        var slice1 = mem.Slice(2);
        // Act - Slice starting at index 2, length 2 (values 20, 30)
        var slice2 = mem.Slice(2, 2);

        // Assert
        Assert.Equal(5, slice1.Length);
        Assert.Equal(20, *slice1.Ptr);

        Assert.Equal(2, slice2.Length);
        Assert.Equal(20, *slice2.Ptr);
        Assert.Equal(30, slice2[1]);

        // Verify owner is preserved
        Assert.Equal(buffer, slice1.MemoryOwner);
        Assert.Equal(buffer, slice2.MemoryOwner);
    }

    // -------------------------------------------------------------------------
    // EQUALITY TESTS
    // -------------------------------------------------------------------------

    [Fact]
    public void BeEqual_WhenPointerAndLengthMatch()
    {
        // Arrange
        byte[] buffer = new byte[10];

        // Act
        var mem1 = new PinnedMemory<byte>(buffer, 0, 5);
        var mem2 = new PinnedMemory<byte>(buffer, 0, 5);
        var mem3 = new PinnedMemory<byte>(buffer, 1, 5); // Different ptr

        // Assert
        Assert.True(mem1.Equals(mem2));
        Assert.False(mem1.Equals(mem3));
    }
}