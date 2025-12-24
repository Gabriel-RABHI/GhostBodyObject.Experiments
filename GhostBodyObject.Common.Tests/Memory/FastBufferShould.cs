using System;
using System.Runtime.CompilerServices; // For Unsafe
using System.Runtime.InteropServices;  // For MemoryMarshal, GCHandle
using Xunit;

// Namespace depends on your project structure
namespace GhostBodyObject.Common.Tests.Memory
{
    public class FastBufferShould
    {
        // -------------------------------------------------------------------------
        // TEST HELPERS (Structs)
        // -------------------------------------------------------------------------

        struct Vector3
        {
            public float X, Y, Z;
        }

        struct MixedStruct
        {
            public byte Header;
            public int ID;
            public double Value;
        }

        // -------------------------------------------------------------------------
        // 1. POINTER OPERATIONS (unsafe byte*)
        // -------------------------------------------------------------------------

        [Fact]
        public unsafe void Pointers_ReadWrite_Aligned()
        {
            // Arrange
            byte[] buffer = new byte[16];
            int expected = 42;

            fixed (byte* ptr = buffer)
            {
                // Act
                FastBuffer.Set(ptr, 0, expected);
                int result = FastBuffer.Get<int>(ptr, 0);

                // Assert
                Assert.Equal(expected, result);
            }

            // Verify underlying memory changed
            Assert.Equal(42, BitConverter.ToInt32(buffer, 0));
        }

        [Fact]
        public unsafe void Pointers_ReadWrite_Unaligned()
        {
            // Arrange
            byte[] buffer = new byte[16];
            long expected = 123456789L;

            fixed (byte* ptr = buffer)
            {
                // Act - Write at offset 1 (unaligned for long)
                FastBuffer.Set(ptr, 1, expected);
                long result = FastBuffer.Get<long>(ptr, 1);

                // Assert
                Assert.Equal(expected, result);
            }
        }

        // -------------------------------------------------------------------------
        // 2. BYTE ARRAY OPERATIONS
        // -------------------------------------------------------------------------

        [Fact]
        public void ByteArray_SetGet_Primitives()
        {
            // Arrange
            byte[] buffer = new byte[100];
            double val = 123.456;

            // Act
            FastBuffer.Set(buffer, 10, val);
            double result = FastBuffer.Get<double>(buffer, 10);

            // Assert
            Assert.Equal(val, result);
        }

        [Fact]
        public void ByteArray_SetGet_Structs()
        {
            // Arrange
            byte[] buffer = new byte[100];
            var vec = new Vector3 { X = 1.1f, Y = 2.2f, Z = 3.3f };

            // Act
            FastBuffer.Set(buffer, 5, vec);
            var result = FastBuffer.Get<Vector3>(buffer, 5);

            // Assert
            Assert.Equal(vec.X, result.X);
            Assert.Equal(vec.Y, result.Y);
            Assert.Equal(vec.Z, result.Z);
        }

        [Fact]
        public void ByteArray_Unaligned_Access()
        {
            // Arrange
            byte[] buffer = new byte[10];
            int value = 0xFFFFFF; // 16777215 (uses 3 bytes effectively, occupies 4)

            // Act
            FastBuffer.Set(buffer, 1, value);
            int result = FastBuffer.Get<int>(buffer, 1);

            // Assert
            Assert.Equal(value, result);

            // Verify index 0 was untouched
            Assert.Equal(0, buffer[0]);
        }

        // -------------------------------------------------------------------------
        // 3. SPAN OPERATIONS
        // -------------------------------------------------------------------------

        [Fact]
        public void Span_SetGet_WorksOnSlices()
        {
            // Arrange
            byte[] buffer = new byte[50];
            Span<byte> slice = buffer.AsSpan().Slice(10);
            int val = 999;

            // Act
            FastBuffer.Set(slice, 0, val);
            int result = FastBuffer.Get<int>(slice, 0);

            // Assert
            Assert.Equal(val, result);

            // Verify underlying array at correct offset
            Assert.Equal(999, BitConverter.ToInt32(buffer, 10));
        }

        // -------------------------------------------------------------------------
        // 4. PINNED MEMORY OPERATIONS
        // -------------------------------------------------------------------------

        [Fact]
        public void PinnedMemory_EndToEnd()
        {
            // Arrange
            byte[] rawBuffer = new byte[128];
            GCHandle handle = GCHandle.Alloc(rawBuffer, GCHandleType.Pinned);

            try
            {
                unsafe
                {
                    // Create PinnedMemory manually for test
                    var pinned = new PinnedMemory<byte>(rawBuffer, (byte*)handle.AddrOfPinnedObject(), rawBuffer.Length);
                    var expected = new MixedStruct { Header = 255, ID = 5000, Value = 3.14159 };

                    // Act
                    FastBuffer.Set(pinned, 2, expected);
                    var result = FastBuffer.Get<MixedStruct>(pinned, 2);

                    // Assert
                    Assert.Equal(expected.Header, result.Header);
                    Assert.Equal(expected.ID, result.ID);
                    Assert.Equal(expected.Value, result.Value);
                }
            }
            finally
            {
                handle.Free();
            }
        }

        // -------------------------------------------------------------------------
        // 5. BULK COPY OPERATIONS
        // -------------------------------------------------------------------------

        [Fact]
        public void WriteArray_StructsToBytes()
        {
            // Arrange
            int count = 5;
            var source = new Vector3[count];
            for (int i = 0; i < count; i++)
                source[i] = new Vector3 { X = i, Y = i, Z = i };

            byte[] dest = new byte[100];

            // Act
            FastBuffer.WriteArray(dest, 10, source);

            // Assert
            // Check first float (X=0)
            Assert.Equal(0, BitConverter.ToSingle(dest, 10));

            // Check last struct (index 4), Z component. 
            // Offset = 10 + (4 structs * 12 bytes) + 8 bytes (offset of Z)
            Assert.Equal(4, BitConverter.ToSingle(dest, 10 + (4 * 12) + 8));
        }

        [Fact]
        public void ReadArray_BytesToStructs()
        {
            // Arrange
            byte[] sourceBytes = new byte[100];

            // Manually write struct data
            Unsafe.WriteUnaligned(ref sourceBytes[0], new Vector3 { X = 10, Y = 20, Z = 30 });
            Unsafe.WriteUnaligned(ref sourceBytes[12], new Vector3 { X = 40, Y = 50, Z = 60 });

            Vector3[] dest = new Vector3[2];

            // Act
            FastBuffer.ReadArray(sourceBytes, 0, dest);

            // Assert
            Assert.Equal(10, dest[0].X);
            Assert.Equal(60, dest[1].Z);
        }

        [Fact]
        public void WriteSpan_SliceToSlice()
        {
            // Arrange
            var source = new int[] { 10, 20, 30, 40 };
            byte[] dest = new byte[100];

            // Act
            FastBuffer.WriteSpan(dest.AsSpan(), 0, source.AsSpan());

            // Assert
            Assert.Equal(10, BitConverter.ToInt32(dest, 0));
            Assert.Equal(20, BitConverter.ToInt32(dest, 4));
        }

        // -------------------------------------------------------------------------
        // 6. STRESS & SAFETY CHECKS
        // -------------------------------------------------------------------------

        [Fact]
        public void Alignment_StressTest()
        {
            // Arrange
            byte[] buffer = new byte[50];
            Array.Fill(buffer, (byte)0xFF);
            short val = 0x1122; // 2 bytes

            // Act - Write at index 1
            FastBuffer.Set(buffer, 1, val);

            // Assert
            Assert.Equal(0xFF, buffer[0]);        // Index 0 should be untouched
            Assert.NotEqual(0xFF, buffer[1]);     // Index 1 should change
            Assert.NotEqual(0xFF, buffer[2]);     // Index 2 should change
            Assert.Equal(0xFF, buffer[3]);        // Index 3 should be untouched

            Assert.Equal(val, FastBuffer.Get<short>(buffer, 1));
        }
    }
}