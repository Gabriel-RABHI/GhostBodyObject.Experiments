using GhostBodyObject.Common.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Entities.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Ghost.Values;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.Entities.Arrays
{
    // ---------------------------------------------------------
    // The ArraysAsStringsAndSpansSmall Entity (Uses ArrayMapSmallEntry)
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 42)]
    public sealed class ArraysAsStringsAndSpansSmall : TestModelBodyBase
    {
        public const int ModelVersion = 1;

        internal unsafe ArraysAsStringsAndSpansSmall_VectorTable* _vTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ArraysAsStringsAndSpansSmall_VectorTable*)_vTablePtr;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _vTablePtr = (IntPtr)value;
        }

        public ArraysAsStringsAndSpansSmall()
        {
            unsafe
            {
                VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansSmall>.BuildStandaloneVersion(ModelVersion, this);
            }
        }

        public ArraysAsStringsAndSpansSmall(PinnedMemory<byte> ghost, bool mapped = true)
        {
            unsafe
            {
                if (mapped)
                    VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansSmall>.BuildMappedVersion(ghost, this, Transaction.IsReadOnly);
                else
                    VectorTableRegistry<TestModelRepository, ArraysAsStringsAndSpansSmall>.BuildStandaloneVersion(ghost, this);
            }
        }

        public unsafe DateTime OneDateTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                return _data.Get<DateTime>(_vTable->OneDateTime_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
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
                    var arrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Guids_MapEntryOffset);
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
                        _vTable->Guids_Setter(this, value);
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
                    var arrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->DateTimes_MapEntryOffset);
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
                        _vTable->DateTimes_Setter(this, value);
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
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->StringU16_MapEntryOffset);
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
                        _vTable->StringU16_Setter(this, value);
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
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->StringU8_MapEntryOffset);
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
                        _vTable->StringU8_Setter(this, value);
                    }
                }
            }
        }
    }


    // ---------------------------------------------------------
    // VECTOR TABLE BUILDER (V1 to V1 Mapping)
    // ---------------------------------------------------------
    public unsafe static class ArraysAsStringsAndSpansSmall_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(TestModelRepository);

        public static Type BodyType => typeof(ArraysAsStringsAndSpansSmall);

        public static int TypeIdentifier => 10;

        public static int TargetVersion => ArraysAsStringsAndSpansSmall.ModelVersion;

        public static VectorTableRecord GetTableRecord()
        {
            if (_initialized)
                return _record;
            _record = new VectorTableRecord();
            _record.Standalone = (VectorTableHeader*)GetStandalone();

            _record.GhostSize = _record.Standalone->MinimalGhostSize;

            var buff = GC.AllocateArray<byte>(_record.GhostSize, true);
            _record.InitialGhost = new PinnedMemory<byte>(buff, 0, buff.Length);

            InitializeGhost(_record.InitialGhost, (ArraysAsStringsAndSpansSmall_VectorTable*)_record.Standalone);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
            _initialized = true;
            return _record;
        }

        #region COMMON
        public static ArraysAsStringsAndSpansSmall_VectorTable* GetCommon()
        {
            var vt = (ArraysAsStringsAndSpansSmall_VectorTable*)NativeMemory.Alloc((nuint)sizeof(ArraysAsStringsAndSpansSmall_VectorTable));
            var f = new GhostHeaderIncrementor();

            // -------- FIELDS SEQUENCE -------- //
            vt->OneDateTime_FieldOffset = f.Push<DateTime>();    // OneDateTime : 8 bytes
            vt->OneInt_FieldOffset = f.Push<int>();              // OneInt : 4 bytes
            f.Padd(4); // Padding to align

            // -------- ARRAY MAP ENTRIES SPACE RESERVATION -------- //
            vt->Guids_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->DateTimes_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->StringU16_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->StringU8_MapEntryOffset = f.Push<ArrayMapSmallEntry>();

            // -------- ARRAY MAP ENTRIES INDEXES -------- //
            vt->Guids_MapEntryIndex = 0;
            vt->DateTimes_MapEntryIndex = 1;
            vt->StringU16_MapEntryIndex = 2;
            vt->StringU8_MapEntryIndex = 3;

            // -------- STANDARD FIELDS -------- //
            vt->Std.TypeCombo = new GhostTypeCombo(GhostIdKind.Entity, (ushort)TypeIdentifier);
            vt->Std.ModelVersion = (short)TargetVersion;
            vt->Std.ReadOnly = false;
            vt->Std.LargeArrays = false;
            vt->Std.ArrayMapOffset = vt->Guids_MapEntryOffset;
            vt->Std.ArrayMapLength = 4; // 4 array properties

            // -------- LENGTH -------- //
            vt->Std.MinimalGhostSize = f.Offset;
            return vt;
        }

        public static void InitializeGhost(PinnedMemory<byte> ghost, ArraysAsStringsAndSpansSmall_VectorTable* _vTable)
        {
            var h = (GhostHeader*)ghost.Ptr;
            h->Initialize((ushort)TargetVersion);
            h->Id = GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier);

            var emptyGuid = new ArrayMapSmallEntry()
            {
                ArrayLength = 0,
                ValueSize = (uint)sizeof(Guid),
                ArrayOffset = (ushort)_vTable->Std.MinimalGhostSize
            };

            var emptyDateTime = new ArrayMapSmallEntry()
            {
                ArrayLength = 0,
                ValueSize = (uint)sizeof(DateTime),
                ArrayOffset = (ushort)_vTable->Std.MinimalGhostSize
            };

            var emptyStringU16 = new ArrayMapSmallEntry()
            {
                ArrayLength = 0,
                ValueSize = sizeof(char),
                ArrayOffset = (ushort)_vTable->Std.MinimalGhostSize
            };

            var emptyStringU8 = new ArrayMapSmallEntry()
            {
                ArrayLength = 0,
                ValueSize = sizeof(byte),
                ArrayOffset = (ushort)_vTable->Std.MinimalGhostSize
            };

            // Initialize all 4 array properties as empty
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Guids_MapEntryOffset) = emptyGuid;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->DateTimes_MapEntryOffset) = emptyDateTime;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->StringU16_MapEntryOffset) = emptyStringU16;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->StringU8_MapEntryOffset) = emptyStringU8;
        }
        #endregion

        #region STANDALONE
        public static ArraysAsStringsAndSpansSmall_VectorTable* GetStandalone()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            // Values Setters
            vt->OneDateTime_Setter = &Standalone_OneDateTime_Setter;
            vt->OneInt_Setter = &Standalone_OneInt_Setter;

            // Arrays Setters
            vt->Guids_Setter = &Guids_Setter;
            vt->DateTimes_Setter = &DateTimes_Setter;
            vt->StringU16_Setter = &StringU16_Setter;
            vt->StringU8_Setter = &StringU8_Setter;

            return vt;
        }

        // ---------------------------------------------------------
        // Value Setters
        // ---------------------------------------------------------
        public static unsafe void Standalone_OneDateTime_Setter(ArraysAsStringsAndSpansSmall body, DateTime value)
            => body._data.Set<DateTime>(body._vTable->OneDateTime_FieldOffset, value);

        public static unsafe void Standalone_OneInt_Setter(ArraysAsStringsAndSpansSmall body, int value)
            => body._data.Set<int>(body._vTable->OneInt_FieldOffset, value);

        // ---------------------------------------------------------
        // Arrays Setters
        // ---------------------------------------------------------
        public static unsafe void Guids_Setter(ArraysAsStringsAndSpansSmall body, GhostSpan<Guid> src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Guids_MapEntryIndex);

        public static unsafe void DateTimes_Setter(ArraysAsStringsAndSpansSmall body, GhostSpan<DateTime> src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->DateTimes_MapEntryIndex);

        public static unsafe void StringU16_Setter(ArraysAsStringsAndSpansSmall body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->StringU16_MapEntryIndex);

        public static unsafe void StringU8_Setter(ArraysAsStringsAndSpansSmall body, GhostStringUtf8 src)
            => BodyBase.SwapAnyArray(body, src.AsBytes(), body._vTable->StringU8_MapEntryIndex);
        #endregion
    }

    // ---------------------------------------------------------
    // VECTOR TABLE STRUCTURE
    // ---------------------------------------------------------
    public unsafe struct ArraysAsStringsAndSpansSmall_VectorTable
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
        // Value Setters
        // ---------------------------------------------------------
        public delegate*<ArraysAsStringsAndSpansSmall, DateTime, void> OneDateTime_Setter;

        public delegate*<ArraysAsStringsAndSpansSmall, int, void> OneInt_Setter;

        // ---------------------------------------------------------
        // Arrays Setters
        // ---------------------------------------------------------
        public delegate*<ArraysAsStringsAndSpansSmall, GhostSpan<Guid>, void> Guids_Setter;

        public delegate*<ArraysAsStringsAndSpansSmall, GhostSpan<DateTime>, void> DateTimes_Setter;

        public delegate*<ArraysAsStringsAndSpansSmall, GhostStringUtf16, void> StringU16_Setter;

        public delegate*<ArraysAsStringsAndSpansSmall, GhostStringUtf8, void> StringU8_Setter;
    }
}
