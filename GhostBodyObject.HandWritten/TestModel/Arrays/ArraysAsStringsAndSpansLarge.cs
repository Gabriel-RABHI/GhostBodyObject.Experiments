using GhostBodyObject.Common.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.TestModel.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Ghost.Values;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.TestModel.Arrays
{
    // ---------------------------------------------------------
    // The ArraysAsStringsAndSpansLarge Entity (Uses ArrayMapLargeEntry)
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 42)]
    public sealed class ArraysAsStringsAndSpansLarge : TestModelBodyBase
    {
        public int ModelVersion => 1;

        internal unsafe ArraysAsStringsAndSpansLarge_VectorTable* _vTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ArraysAsStringsAndSpansLarge_VectorTable*)_vTablePtr;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _vTablePtr = (IntPtr)value;
        }

        public ArraysAsStringsAndSpansLarge()
        {
            unsafe
            {
                VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansLarge>.BuildInitialVersion(ModelVersion, this);
            }
        }

        public ArraysAsStringsAndSpansLarge(PinnedMemory<byte> ghost, bool mapped = true)
        {
            unsafe
            {
                if (mapped)
                    VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansLarge>.BuildMappedVersion(ghost, this, Transaction.IsReadOnly);
                else
                    VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansLarge>.BuildStandaloneVersion(ghost, this);
            }
        }

        public unsafe DateTime OneDateTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_immutable)
                    GuardLocalScope();
                return _data.Get<DateTime>(_vTable->OneDateTime_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_immutable)
                    throw new InvalidOperationException("Cannot modify an immutable Body object.");
                using (GuardWriteScope())
                {
                    _vTable->OneDateTime_Setter(this, value);
                }
            }
        }

        public unsafe int OneInt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                return _data.Get<int>(_vTable->OneInt_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                using (GuardWriteScope())
                {
                    _vTable->OneInt_Setter(this, value);
                }
            }
        }

        public GhostSpan<Guid> Guids
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var arrayEntry = _data.Get<ArrayMapLargeEntry>(_vTable->Guids_MapEntryOffset);
                    return new GhostSpan<Guid>(this, _vTable->Guids_MapEntryIndex, _data.Slice((int)arrayEntry.ArrayOffset, arrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Guids_MapEntryIndex);
                    }
                }
            }
        }

        public GhostSpan<DateTime> DateTimes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var arrayEntry = _data.Get<ArrayMapLargeEntry>(_vTable->DateTimes_MapEntryOffset);
                    return new GhostSpan<DateTime>(this, _vTable->DateTimes_MapEntryIndex, _data.Slice((int)arrayEntry.ArrayOffset, arrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->DateTimes_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 StringU16
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapLargeEntry>(_vTable->StringU16_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->StringU16_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->StringU16_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf8 StringU8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapLargeEntry>(_vTable->StringU8_MapEntryOffset);
                    return new GhostStringUtf8(this, _vTable->StringU8_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), value.AsBytes(), _vTable->StringU8_MapEntryIndex);
                    }
                }
            }
        }
    }


    // ---------------------------------------------------------
    // VECTOR TABLE BUILDER (V1 to V1 Mapping)
    // ---------------------------------------------------------
    public unsafe static class ArraysAsStringsAndSpansLarge_V1_V1_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(TestModelRepository);

        public static Type BodyType => typeof(ArraysAsStringsAndSpansLarge);

        public static int TypeIdentifier => 11;

        public static int SourceVersion => 1;

        public static int TargetVersion => 1;

        public static VectorTableRecord GetTableRecord()
        {
            if (_initialized)
                return _record;
            _record = new VectorTableRecord();
            _record.Initial = (VectorTableHeader*)GetInitial();
            _record.Standalone = (VectorTableHeader*)GetStandalone();

            _record.GhostSize = _record.Standalone->MinimalGhostSize;

            var buff = GC.AllocateArray<byte>(_record.GhostSize, true);
            _record.InitialGhost = new PinnedMemory<byte>(buff, 0, buff.Length);

            InitializeGhost(_record.InitialGhost, (ArraysAsStringsAndSpansLarge_VectorTable*)_record.Initial);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
            _initialized = true;
            return _record;
        }

        #region COMMON
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetCommon()
        {
            var vt = (ArraysAsStringsAndSpansLarge_VectorTable*)NativeMemory.Alloc((nuint)sizeof(ArraysAsStringsAndSpansLarge_VectorTable));
            var f = new GhostHeaderIncrementor();

            // -------- FIELDS SEQUENCE -------- //
            vt->OneDateTime_FieldOffset = f.Push<DateTime>();    // OneDateTime : 8 bytes
            vt->OneInt_FieldOffset = f.Push<int>();              // OneInt : 4 bytes
            f.Padd(4); // Padding to align to 8 bytes for ArrayMapLargeEntry

            // -------- ARRAY MAP ENTRIES SPACE RESERVATION -------- //
            // 4 array properties, each with an ArrayMapLargeEntry (8 bytes each)
            vt->Guids_MapEntryOffset = f.Push<ArrayMapLargeEntry>();
            vt->DateTimes_MapEntryOffset = f.Push<ArrayMapLargeEntry>();
            vt->StringU16_MapEntryOffset = f.Push<ArrayMapLargeEntry>();
            vt->StringU8_MapEntryOffset = f.Push<ArrayMapLargeEntry>();

            // -------- ARRAY MAP ENTRIES INDEXES -------- //
            vt->Guids_MapEntryIndex = 0;
            vt->DateTimes_MapEntryIndex = 1;
            vt->StringU16_MapEntryIndex = 2;
            vt->StringU8_MapEntryIndex = 3;

            // -------- STANDARD FIELDS -------- //
            vt->Std.TypeCombo = new GhostId(GhostIdKind.Entity, (ushort)TypeIdentifier, default, default).TypeCombo;
            vt->Std.ModelVersion = (short)SourceVersion;
            vt->Std.ReadOnly = false;
            vt->Std.LargeArrays = true; // Using ArrayMapLargeEntry
            vt->Std.ArrayMapOffset = vt->Guids_MapEntryOffset;
            vt->Std.ArrayMapLength = 4; // 4 array properties

            // -------- LENGTH -------- //
            vt->Std.MinimalGhostSize = f.Offset;
            return vt;
        }

        public static void InitializeGhost(PinnedMemory<byte> ghost, ArraysAsStringsAndSpansLarge_VectorTable* _vTable)
        {
            var h = (GhostHeader*)ghost.Ptr;
            h->Id = GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier);

            var emptyGuid = new ArrayMapLargeEntry()
            {
                ArrayLength = 0,
                ValueSize = (byte)sizeof(Guid),
                ArrayOffset = (uint)_vTable->Std.MinimalGhostSize
            };

            var emptyDateTime = new ArrayMapLargeEntry()
            {
                ArrayLength = 0,
                ValueSize = (byte)sizeof(DateTime),
                ArrayOffset = (uint)_vTable->Std.MinimalGhostSize
            };

            var emptyStringU16 = new ArrayMapLargeEntry()
            {
                ArrayLength = 0,
                ValueSize = sizeof(char),
                ArrayOffset = (uint)_vTable->Std.MinimalGhostSize
            };

            var emptyStringU8 = new ArrayMapLargeEntry()
            {
                ArrayLength = 0,
                ValueSize = sizeof(byte),
                ArrayOffset = (uint)_vTable->Std.MinimalGhostSize
            };

            // Initialize all 4 array properties as empty
            *(ArrayMapLargeEntry*)(ghost.Ptr + _vTable->Guids_MapEntryOffset) = emptyGuid;
            *(ArrayMapLargeEntry*)(ghost.Ptr + _vTable->DateTimes_MapEntryOffset) = emptyDateTime;
            *(ArrayMapLargeEntry*)(ghost.Ptr + _vTable->StringU16_MapEntryOffset) = emptyStringU16;
            *(ArrayMapLargeEntry*)(ghost.Ptr + _vTable->StringU8_MapEntryOffset) = emptyStringU8;
        }
        #endregion

        #region INITIAL
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetInitial()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Std.SwapAnyArray = &Initial_SwapAnyArray;
            vt->Std.AppendToArray = &Initial_AppendToArray;
            vt->Std.PrependToArray = &Initial_PrependToArray;
            vt->Std.InsertIntoArray = &Initial_InsertIntoArray;
            vt->Std.RemoveFromArray = &Initial_RemoveFromArray;
            vt->Std.ReplaceInArray = &Initial_ReplaceInArray;
            vt->OneDateTime_Setter = &Initial_OneDateTime_Setter;
            vt->OneInt_Setter = &Initial_OneInt_Setter;
            return vt;
        }

        public static ArraysAsStringsAndSpansLarge InitialToStandalone(ArraysAsStringsAndSpansLarge body)
        {
            var union = Unsafe.As<BodyUnion>(body);
            union._data = TransientGhostMemoryAllocator.Allocate(union._data.Length);
            union._vTablePtr = (nint)_record.Standalone;
            _record.InitialGhost.CopyTo(union._data);
            union._data.Set<GhostId>(0, GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier));
            return body;
        }

        public static unsafe void Initial_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).SwapAnyArray(src, arrayIndex);

        public static unsafe void Initial_AppendToArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).AppendToArray(src, arrayIndex);

        public static unsafe void Initial_PrependToArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).PrependToArray(src, arrayIndex);

        public static unsafe void Initial_InsertIntoArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex, int byteOffset)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).InsertIntoArray(src, arrayIndex, byteOffset);

        public static unsafe void Initial_RemoveFromArray(BodyUnion body, int arrayIndex, int byteOffset, int byteLength)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).RemoveFromArray(arrayIndex, byteOffset, byteLength);

        public static unsafe void Initial_ReplaceInArray(BodyUnion body, ReadOnlySpan<byte> replacement, int arrayIndex, int byteOffset, int byteLengthToRemove)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).ReplaceInArray(replacement, arrayIndex, byteOffset, byteLengthToRemove);

        public static unsafe void Initial_OneDateTime_Setter(ArraysAsStringsAndSpansLarge body, DateTime value)
            => Standalone_OneDateTime_Setter(InitialToStandalone(body), value);

        public static unsafe void Initial_OneInt_Setter(ArraysAsStringsAndSpansLarge body, int value)
            => Standalone_OneInt_Setter(InitialToStandalone(body), value);
        #endregion

        #region STANDALONE
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetStandalone()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Std.SwapAnyArray = &Standalone_SwapAnyArray;
            vt->Std.AppendToArray = &Standalone_AppendToArray;
            vt->Std.PrependToArray = &Standalone_PrependToArray;
            vt->Std.InsertIntoArray = &Standalone_InsertIntoArray;
            vt->Std.RemoveFromArray = &Standalone_RemoveFromArray;
            vt->Std.ReplaceInArray = &Standalone_ReplaceInArray;
            vt->OneDateTime_Setter = &Standalone_OneDateTime_Setter;
            vt->OneInt_Setter = &Standalone_OneInt_Setter;
            return vt;
        }

        public static unsafe void Standalone_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).SwapAnyArray(src, arrayIndex);

        public static unsafe void Standalone_AppendToArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).AppendToArray(src, arrayIndex);

        public static unsafe void Standalone_PrependToArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).PrependToArray(src, arrayIndex);

        public static unsafe void Standalone_InsertIntoArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex, int byteOffset)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).InsertIntoArray(src, arrayIndex, byteOffset);

        public static unsafe void Standalone_RemoveFromArray(BodyUnion body, int arrayIndex, int byteOffset, int byteLength)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).RemoveFromArray(arrayIndex, byteOffset, byteLength);

        public static unsafe void Standalone_ReplaceInArray(BodyUnion body, ReadOnlySpan<byte> replacement, int arrayIndex, int byteOffset, int byteLengthToRemove)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).ReplaceInArray(replacement, arrayIndex, byteOffset, byteLengthToRemove);

        public static unsafe void Standalone_OneDateTime_Setter(ArraysAsStringsAndSpansLarge body, DateTime value)
            => Unsafe.As<BodyUnion>(body)._data.Set<DateTime>(body._vTable->OneDateTime_FieldOffset, value);

        public static unsafe void Standalone_OneInt_Setter(ArraysAsStringsAndSpansLarge body, int value)
            => Unsafe.As<BodyUnion>(body)._data.Set<int>(body._vTable->OneInt_FieldOffset, value);
        #endregion
    }

    // ---------------------------------------------------------
    // VECTOR TABLE STRUCTURE
    // ---------------------------------------------------------
    public unsafe struct ArraysAsStringsAndSpansLarge_VectorTable
    {
        // ---------------------------------------------------------
        // Standard Fields
        // ---------------------------------------------------------
        public VectorTableHeader Std;

        // ---------------------------------------------------------
        // Value Field Offsets
        // ---------------------------------------------------------
        public int OneDateTime_FieldOffset;

        public int OneInt_FieldOffset;

        // ---------------------------------------------------------
        // Array Map Offsets
        // ---------------------------------------------------------
        public int Guids_MapEntryOffset;

        public int DateTimes_MapEntryOffset;

        public int StringU16_MapEntryOffset;

        public int StringU8_MapEntryOffset;

        // -------- Indexes for quick access
        public int Guids_MapEntryIndex;

        public int DateTimes_MapEntryIndex;

        public int StringU16_MapEntryIndex;

        public int StringU8_MapEntryIndex;

        // ---------------------------------------------------------
        // Setters
        // ---------------------------------------------------------
        public delegate*<ArraysAsStringsAndSpansLarge, DateTime, void> OneDateTime_Setter;

        public delegate*<ArraysAsStringsAndSpansLarge, int, void> OneInt_Setter;
    }
}
