using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Provides a lightweight, unsafe wrapper for a contiguous block of pinned, unmanaged memory, enabling direct access
/// and manipulation of its contents.
/// </summary>
/// <remarks>PinnedMemory<T> allows efficient, low-level access to unmanaged memory regions while optionally
/// tracking ownership. It is intended for advanced scenarios where performance and direct memory manipulation are
/// required, such as interop or custom memory management. The struct does not perform bounds checking or memory safety
/// validation; callers are responsible for ensuring correct usage. The memory referenced by this struct must remain
/// valid and pinned for the lifetime of the PinnedMemory<T> instance. This type is not thread safe.</remarks>
/// <typeparam name="T">The type of elements in the pinned memory block. Must be an unmanaged type.</typeparam>
public readonly unsafe struct PinnedMemory<T> : IEquatable<PinnedMemory<T>> where T : unmanaged
{
    internal readonly object? _owner;
    internal readonly T* _ptr;
    internal readonly int _length;

    // -------------------------------------------------------------------------
    // CONSTRUCTORS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the PinnedMemory class with the specified owner, memory pointer, and length.
    /// </summary>
    /// <param name="owner">An optional object that represents the owner of the pinned memory block. Can be null if ownership tracking is
    /// not required.</param>
    /// <param name="pointer">A pointer to the start of the memory block to be pinned. Must not be null.</param>
    /// <param name="length">The number of elements in the pinned memory block. Must be greater than or equal to zero.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinnedMemory(object? owner, void* pointer, int length)
    {
        _owner = owner;
        _ptr = (T*)pointer;
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of the PinnedMemory class, providing a pinned reference to a segment of the specified
    /// byte array.
    /// </summary>
    /// <remarks>The pinned memory segment allows for direct, unsafe access to the underlying array data. The
    /// caller is responsible for ensuring that the array remains valid and is not modified in a way that would
    /// invalidate the pinned segment during its lifetime. This constructor is intended for advanced scenarios requiring
    /// interoperability or performance optimizations.</remarks>
    /// <param name="owner">The byte array that owns the memory to be pinned. Cannot be null.</param>
    /// <param name="offset">The zero-based index within the array at which the pinned memory segment begins. Must be non-negative and less
    /// than the length of the array.</param>
    /// <param name="length">The number of bytes to include in the pinned memory segment. Must be non-negative and the range defined by
    /// offset and length must not exceed the bounds of the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinnedMemory(byte[] owner, int offset, int length)
    {
        _owner = owner;
        _length = length;
        ref byte byteRef = ref MemoryMarshal.GetArrayDataReference(owner);
        ref byte offsetRef = ref Unsafe.Add(ref byteRef, offset);
        _ptr = (T*)Unsafe.AsPointer(ref offsetRef);
    }

    /// <summary>
    /// Initializes a new instance of the PinnedMemory class that provides access to a contiguous region of the
    /// specified array, starting at the given offset and spanning the specified length.
    /// </summary>
    /// <remarks>The pinned region allows for direct memory access to the specified segment of the array. The
    /// caller is responsible for ensuring that the array remains valid for the lifetime of the PinnedMemory instance.
    /// This constructor does not perform bounds checking; invalid arguments may result in undefined behavior.</remarks>
    /// <param name="owner">The array to pin and provide access to. Cannot be null.</param>
    /// <param name="offset">The zero-based index in the array at which the pinned region begins. Must be greater than or equal to zero and
    /// less than the length of the array.</param>
    /// <param name="length">The number of elements in the pinned region. Must be greater than or equal to zero, and the sum of offset and
    /// length must not exceed the length of the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinnedMemory(T[] owner, int offset, int length)
    {
        _owner = owner;
        _length = length;
        ref T startRef = ref MemoryMarshal.GetArrayDataReference(owner);
        ref T offsetRef = ref Unsafe.Add(ref startRef, offset);
        _ptr = (T*)Unsafe.AsPointer(ref offsetRef);
    }

    // -------------------------------------------------------------------------
    // FAST ACCESS (No Pinning, No Managers)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets a value indicating whether the collection contains no elements.
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Gets the raw pointer to the start of the pinned memory block.
    /// </summary>
    public T* Ptr => _ptr;

    public object? MemoryOwner => _owner;

    /// <summary>
    /// Gets a reference to the element at the specified index within the collection.
    /// </summary>
    /// <remarks>Accessing an index outside the bounds of the collection may result in undefined behavior. The
    /// returned reference allows direct modification of the underlying element.</remarks>
    /// <param name="index">The zero-based index of the element to retrieve. Must be within the bounds of the collection.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Optional: Add bounds checking via Debug.Assert or if block
            return ref Unsafe.AsRef<T>(_ptr + index);
        }
    }

    /// <summary>
    /// Writes a value of the specified struct type at the given byte offset from the base pointer.
    /// </summary>
    /// <remarks>This method performs an unsafe write operation directly to memory. The caller must ensure
    /// that the offset is within the bounds of the allocated memory region and that the memory is properly aligned for
    /// the type T. Incorrect usage may result in data corruption or application crashes.</remarks>
    /// <typeparam name="T">The value type to write. Must be an unmanaged struct.</typeparam>
    /// <param name="offset">The byte offset from the base pointer at which to write the value.</param>
    /// <param name="value">The value to write at the specified offset.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Set<T>(int offset, T value) where T : struct
        => *(T*)(_ptr + offset) = value;

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the underlying memory at the specified byte offset.
    /// </summary>
    /// <remarks>The caller is responsible for ensuring that the offset is within the bounds of the underlying
    /// memory and that the memory is properly aligned for the type <typeparamref name="T"/>. Reading from an invalid
    /// offset or with incorrect alignment may result in undefined behavior.</remarks>
    /// <typeparam name="T">The value type to read from memory. Must be an unmanaged struct.</typeparam>
    /// <param name="offset">The zero-based byte offset from the start of the memory region at which to read the value.</param>
    /// <returns>The value of type <typeparamref name="T"/> read from the specified offset in memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T Get<T>(int offset) where T : struct
        => *(T*)(_ptr + offset);

    /// <summary>
    /// Writes a value of the specified struct type at the given byte offset from the base pointer.
    /// </summary>
    /// <remarks>This method performs an unsafe write operation directly to memory. The caller must ensure
    /// that the offset is within the bounds of the allocated memory region and that the memory is properly aligned for
    /// the type T. Incorrect usage may result in data corruption or application crashes.</remarks>
    /// <typeparam name="T">The value type to write. Must be an unmanaged struct.</typeparam>
    /// <param name="offset">The byte offset from the base pointer at which to write the value.</param>
    /// <param name="value">The value to write at the specified offset.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Set<T>(T value) where T : struct
        => *(T*)_ptr = value;

    /// <summary>
    /// Reads a value of type <typeparamref name="T"/> from the underlying memory at the specified byte offset.
    /// </summary>
    /// <remarks>The caller is responsible for ensuring that the offset is within the bounds of the underlying
    /// memory and that the memory is properly aligned for the type <typeparamref name="T"/>. Reading from an invalid
    /// offset or with incorrect alignment may result in undefined behavior.</remarks>
    /// <typeparam name="T">The value type to read from memory. Must be an unmanaged struct.</typeparam>
    /// <param name="offset">The zero-based byte offset from the start of the memory region at which to read the value.</param>
    /// <returns>The value of type <typeparamref name="T"/> read from the specified offset in memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T Get<T>() where T : struct
        => *(T*)_ptr;

    /// <summary>
    /// Writes the contents of the specified value type array to the underlying memory at the given byte offset.
    /// </summary>
    /// <remarks>The method copies the entire contents of the source array into the underlying memory starting
    /// at the specified offset. The caller is responsible for ensuring that the destination memory has sufficient space
    /// to accommodate the array data. No bounds checking is performed.</remarks>
    /// <typeparam name="T">The value type of the elements to write. Must be a struct.</typeparam>
    /// <param name="destOffset">The zero-based byte offset in the destination memory at which to begin writing the array data.</param>
    /// <param name="source">The array of value type elements to write. Cannot be null.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteArray<T>(int destOffset, T[] source) where T : struct
    {
        byte* destPtr = (byte*)(_ptr + destOffset);
        byte* srcPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(source));
        uint byteCount = (uint)(source.Length * Unsafe.SizeOf<T>());
        Unsafe.CopyBlockUnaligned(destPtr, srcPtr, byteCount);
    }

    /// <summary>
    /// Copies a block of memory from the source buffer at the specified byte offset into the provided destination array
    /// of value types.
    /// </summary>
    /// <remarks>The method copies data as a block of bytes into the destination array, interpreting the
    /// source memory as a sequence of elements of type T. The caller must ensure that the source buffer contains enough
    /// data at the specified offset to fill the destination array. No bounds checking is performed.</remarks>
    /// <typeparam name="T">The value type of the elements to copy into the destination array.</typeparam>
    /// <param name="srcOffset">The zero-based byte offset, relative to the start of the source buffer, from which to begin copying.</param>
    /// <param name="destination">The array that receives the copied data. The length of the array determines how many elements are copied.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadArray<T>(int srcOffset, T[] destination) where T : struct
    {
        byte* srcPtr = (byte*)(_ptr + srcOffset);
        byte* destPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(destination));
        uint byteCount = (uint)(destination.Length * Unsafe.SizeOf<T>());
        Unsafe.CopyBlockUnaligned(destPtr, srcPtr, byteCount);
    }

    /// <summary>
    /// Gets a span representing the contiguous region of memory for the underlying data.
    /// </summary>
    /// <remarks>The returned span provides direct access to the memory buffer. Modifying the span will affect
    /// the underlying data. The span is valid only as long as the parent object remains valid and unmodified.</remarks>
    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<T>(_ptr, _length);
    }

    // -------------------------------------------------------------------------
    // SLICING (Keeps Owner Alive)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new pinned memory region that represents a subrange of the current memory, starting at the specified
    /// index.
    /// </summary>
    /// <remarks>The returned slice shares the same underlying memory owner as the original region. The
    /// original memory region remains valid and is not affected by the slice.</remarks>
    /// <param name="start">The zero-based index at which to begin the slice. Must be greater than or equal to 0 and less than or equal to
    /// the length of the current memory region.</param>
    /// <returns>A new PinnedMemory<T> instance that starts at the specified index and extends to the end of the current memory
    /// region.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinnedMemory<T> Slice(int start)
    {
        return new PinnedMemory<T>(_owner, _ptr + start, _length - start);
    }

    /// <summary>
    /// Creates a new PinnedMemory<T> instance that represents a subrange of the current memory block, starting at the
    /// specified index and spanning the specified length.
    /// </summary>
    /// <param name="start">The zero-based index at which the slice begins. Must be greater than or equal to 0 and less than the length of
    /// the current memory block.</param>
    /// <param name="length">The number of elements in the slice. Must be greater than or equal to 0 and the range defined by start and
    /// length must not exceed the bounds of the current memory block.</param>
    /// <returns>A PinnedMemory<T> instance that represents the specified subrange of the current memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PinnedMemory<T> Slice(int start, int length)
    {
        return new PinnedMemory<T>(_owner, _ptr + start, length);
    }

    public static PinnedMemory<T> Empty => new PinnedMemory<T>(null, null, 0);

    public void CopyTo(PinnedMemory<T> destination)
    {
        uint byteCount = (uint)(_length * Unsafe.SizeOf<T>());
        Unsafe.CopyBlockUnaligned(destination._ptr, _ptr, byteCount);
    }

    // -------------------------------------------------------------------------
    // BOILERPLATE
    // -------------------------------------------------------------------------
    public bool Equals(PinnedMemory<T> other) => _ptr == other._ptr && _length == other._length;
    public override bool Equals(object? obj) => obj is PinnedMemory<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_owner), (IntPtr)_ptr, _length);
}
