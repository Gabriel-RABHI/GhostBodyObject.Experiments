using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{
    // ---------------------------------------------------------
    // VECTOR TABLE BUILDER
    // ---------------------------------------------------------
    public unsafe static class BloggerUser_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(BloggerRepository);

        public static Type BodyType => typeof(BloggerUser);

        public static int TypeIdentifier => 11;

        public static int TargetVersion => BloggerUser.ModelVersion;

        public static VectorTableRecord GetTableRecord()
        {
            if (_initialized)
                return _record;
            _record = new VectorTableRecord();
            _record.Standalone = (VectorTableHeader*)GetStandalone();
            _record.MappedMutable = (VectorTableHeader*)GetMappedMutable();
            _record.MappedReadOnly = (VectorTableHeader*)GetMappedReadOnly();

            _record.GhostSize = _record.Standalone->MinimalGhostSize;

            var buff = GC.AllocateArray<byte>(_record.GhostSize, true);
            _record.InitialGhost = new PinnedMemory<byte>(buff, 0, buff.Length);

            InitializeGhost(_record.InitialGhost, (BloggerUser_VectorTable*)_record.Standalone);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
            _initialized = true;
            return _record;
        }

        #region COMMON
        public static BloggerUser_VectorTable* GetCommon()
        {
            var vt = (BloggerUser_VectorTable*)NativeMemory.Alloc((nuint)sizeof(BloggerUser_VectorTable));
            var f = new GhostHeaderIncrementor();

            // -------- FIELDS SEQUENCE -------- //
            vt->BirthDate_FieldOffset = f.Push<DateTime>();      // BirthDate : 8 bytes
            vt->CustomerCode_FieldOffset = f.Push<int>();        // CustomerCode : 4 bytes
            vt->Active_FieldOffset = f.Push<bool>();             // Active : 1 byte
            f.Padd(4); // Padding to align

            // -------- ARRAY MAP ENTRIES SPACE RESERVATION -------- //
            // 12 string properties, each with an ArrayMapSmallEntry (4 bytes each)
            vt->FirstName_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->LastName_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Pseudonyme_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Presentation_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->City_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Country_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->CompanyName_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Address1_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Address2_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Address3_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->ZipCode_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->Hobbies_MapEntryOffset = f.Push<ArrayMapSmallEntry>();

            // -------- ARRAY MAP ENTRIES INDEXES -------- //
            vt->FirstName_MapEntryIndex = 0;
            vt->LastName_MapEntryIndex = 1;
            vt->Pseudonyme_MapEntryIndex = 2;
            vt->Presentation_MapEntryIndex = 3;
            vt->City_MapEntryIndex = 4;
            vt->Country_MapEntryIndex = 5;
            vt->CompanyName_MapEntryIndex = 6;
            vt->Address1_MapEntryIndex = 7;
            vt->Address2_MapEntryIndex = 8;
            vt->Address3_MapEntryIndex = 9;
            vt->ZipCode_MapEntryIndex = 10;
            vt->Hobbies_MapEntryIndex = 11;

            // -------- STANDARD FIELDS -------- //
            vt->Std.TypeCombo = new GhostTypeCombo(GhostIdKind.Entity, (ushort)TypeIdentifier);
            vt->Std.ModelVersion = (short)TargetVersion;
            vt->Std.ReadOnly = false;
            vt->Std.LargeArrays = false;
            vt->Std.ArrayMapOffset = vt->FirstName_MapEntryOffset;
            vt->Std.ArrayMapLength = 12; // 12 string properties

            // -------- LENGTH -------- //
            vt->Std.MinimalGhostSize = f.Offset;
            return vt;
        }

        public static void InitializeGhost(PinnedMemory<byte> ghost, BloggerUser_VectorTable* _vTable)
        {
            var h = (GhostHeader*)ghost.Ptr;
            h->Initialize((ushort)TargetVersion);
            h->Id = GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier);

            var emptyString = new ArrayMapSmallEntry() {
                ArrayLength = 0,
                ValueSize = sizeof(char),
                ArrayOffset = (ushort)_vTable->Std.MinimalGhostSize
            };

            // Initialize all 12 string properties as empty
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->FirstName_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->LastName_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Pseudonyme_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Presentation_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->City_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Country_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->CompanyName_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Address1_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Address2_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Address3_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->ZipCode_MapEntryOffset) = emptyString;
            *(ArrayMapSmallEntry*)(ghost.Ptr + _vTable->Hobbies_MapEntryOffset) = emptyString;
        }
        #endregion

        #region STANDALONE
        public static BloggerUser_VectorTable* GetStandalone()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            // Values Setters
            vt->Active_Setter = &Standalone_Active_Setter;
            vt->CustomerCode_Setter = &Standalone_CustomerCode_Setter;
            vt->BirthDate_Setter = &Standalone_BirthDate_Setter;

            // Arrays Setters
            vt->FirstName_Setter = &FirstName_Setter_Standalone;
            vt->LastName_Setter = &LastName_Setter_Standalone;
            vt->Pseudonyme_Setter = &Pseudonyme_Setter_Standalone;
            vt->Presentation_Setter = &Presentation_Setter_Standalone;
            vt->City_Setter = &City_Setter_Standalone;
            vt->Country_Setter = &Country_Setter_Standalone;
            vt->CompanyName_Setter = &CompanyName_Setter_Standalone;
            vt->Address1_Setter = &Address1_Setter_Standalone;
            vt->Address2_Setter = &Address2_Setter_Standalone;
            vt->Address3_Setter = &Address3_Setter_Standalone;
            vt->ZipCode_Setter = &ZipCode_Setter_Standalone;
            vt->Hobbies_Setter = &Hobbies_Setter_Standalone;

            return vt;
        }

        // ---------------------------------------------------------
        // Value Setters
        // ---------------------------------------------------------
        public static unsafe void Standalone_Active_Setter(BloggerUser body, bool value)
            => body._data.Set<bool>(body._vTable->Active_FieldOffset, value);

        public static unsafe void Standalone_CustomerCode_Setter(BloggerUser body, int value)
            => body._data.Set<int>(body._vTable->CustomerCode_FieldOffset, value);

        public static unsafe void Standalone_BirthDate_Setter(BloggerUser body, DateTime value)
            => body._data.Set<DateTime>(body._vTable->BirthDate_FieldOffset, value);

        // ---------------------------------------------------------
        // Arrays Setters
        // ---------------------------------------------------------
        public static unsafe void FirstName_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->FirstName_MapEntryIndex);

        public static unsafe void LastName_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->LastName_MapEntryIndex);

        public static unsafe void Pseudonyme_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Pseudonyme_MapEntryIndex);

        public static unsafe void Presentation_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Presentation_MapEntryIndex);

        public static unsafe void City_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->City_MapEntryIndex);

        public static unsafe void Country_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Country_MapEntryIndex);

        public static unsafe void CompanyName_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->CompanyName_MapEntryIndex);

        public static unsafe void Address1_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address1_MapEntryIndex);

        public static unsafe void Address2_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address2_MapEntryIndex);

        public static unsafe void Address3_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address3_MapEntryIndex);

        public static unsafe void ZipCode_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->ZipCode_MapEntryIndex);

        public static unsafe void Hobbies_Setter_Standalone(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body, MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Hobbies_MapEntryIndex);
        #endregion

        #region MAPPED MUTABLE
        public static BloggerUser_VectorTable* GetMappedMutable()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            // Values Setters
            vt->Active_Setter = &Active_Setter_MappedMutable;
            vt->CustomerCode_Setter = &CustomerCode_Setter_MappedMutable;
            vt->BirthDate_Setter = &BirthDate_Setter_MappedMutable;

            // Arrays Setters
            vt->FirstName_Setter = &FirstName_Setter_MappedMutable;
            vt->LastName_Setter = &LastName_Setter_MappedMutable;
            vt->Pseudonyme_Setter = &Pseudonyme_Setter_MappedMutable;
            vt->Presentation_Setter = &Presentation_Setter_MappedMutable;
            vt->City_Setter = &City_Setter_MappedMutable;
            vt->Country_Setter = &Country_Setter_MappedMutable;
            vt->CompanyName_Setter = &CompanyName_Setter_MappedMutable;
            vt->Address1_Setter = &Address1_Setter_MappedMutable;
            vt->Address2_Setter = &Address2_Setter_MappedMutable;
            vt->Address3_Setter = &Address3_Setter_MappedMutable;
            vt->ZipCode_Setter = &ZipCode_Setter_MappedMutable;
            vt->Hobbies_Setter = &Hobbies_Setter_MappedMutable;

            return vt;
        }

        // ---------------------------------------------------------
        // Value Setters
        // ---------------------------------------------------------
        public static unsafe void Active_Setter_MappedMutable(BloggerUser body, bool value)
            => body._data.Set<bool>(body.ToStandalone()._vTable->Active_FieldOffset, value);

        public static unsafe void CustomerCode_Setter_MappedMutable(BloggerUser body, int value)
            => body._data.Set<int>(body.ToStandalone()._vTable->CustomerCode_FieldOffset, value);

        public static unsafe void BirthDate_Setter_MappedMutable(BloggerUser body, DateTime value)
            => body._data.Set<DateTime>(body.ToStandalone()._vTable->BirthDate_FieldOffset, value);

        // ---------------------------------------------------------
        // Arrays Setters
        // ---------------------------------------------------------
        public static unsafe void FirstName_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->FirstName_MapEntryIndex);

        public static unsafe void LastName_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->LastName_MapEntryIndex);

        public static unsafe void Pseudonyme_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Pseudonyme_MapEntryIndex);

        public static unsafe void Presentation_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Presentation_MapEntryIndex);

        public static unsafe void City_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->City_MapEntryIndex);

        public static unsafe void Country_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Country_MapEntryIndex);

        public static unsafe void CompanyName_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->CompanyName_MapEntryIndex);

        public static unsafe void Address1_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address1_MapEntryIndex);

        public static unsafe void Address2_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address2_MapEntryIndex);

        public static unsafe void Address3_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Address3_MapEntryIndex);

        public static unsafe void ZipCode_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->ZipCode_MapEntryIndex);

        public static unsafe void Hobbies_Setter_MappedMutable(BloggerUser body, GhostStringUtf16 src)
            => BodyBase.SwapAnyArray(body.ToStandalone(), MemoryMarshal.AsBytes(src.AsSpan()), body._vTable->Hobbies_MapEntryIndex);

        #endregion

        #region MAPPED MUTABLE
        public static BloggerUser_VectorTable* GetMappedReadOnly()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            // Values Setters
            vt->Active_Setter = &Active_Setter_MappedReadOnly;
            vt->CustomerCode_Setter = &CustomerCode_Setter_MappedReadOnly;
            vt->BirthDate_Setter = &BirthDate_Setter_MappedReadOnly;

            // Arrays Setters
            vt->FirstName_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->LastName_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Pseudonyme_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Presentation_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->City_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Country_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->CompanyName_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Address1_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Address2_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Address3_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->ZipCode_Setter = &GhostStringUtf16_Setter_MappedReadOnly;
            vt->Hobbies_Setter = &GhostStringUtf16_Setter_MappedReadOnly;

            return vt;
        }

        // ---------------------------------------------------------
        // Value Setters
        // ---------------------------------------------------------
        public static unsafe void Active_Setter_MappedReadOnly(BloggerUser body, bool value)
            => throw new InvalidOperationException("Cannot modify read-only body.");

        public static unsafe void CustomerCode_Setter_MappedReadOnly(BloggerUser body, int value)
            => throw new InvalidOperationException("Cannot modify read-only body.");

        public static unsafe void BirthDate_Setter_MappedReadOnly(BloggerUser body, DateTime value)
            => throw new InvalidOperationException("Cannot modify read-only body.");

        // ---------------------------------------------------------
        // Arrays Setters
        // ---------------------------------------------------------
        public static unsafe void GhostStringUtf16_Setter_MappedReadOnly(BloggerUser body, GhostStringUtf16 src)
            => throw new InvalidOperationException("Cannot modify read-only body.");

        #endregion
    }
}
