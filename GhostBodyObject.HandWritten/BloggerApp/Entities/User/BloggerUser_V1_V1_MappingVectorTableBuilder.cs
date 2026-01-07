using GhostBodyObject.Common.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{
    // ---------------------------------------------------------
    // SPECIFIC TO Each ENTITY
    // ---------------------------------------------------------
    public unsafe static class BloggerUser_V1_V1_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(BloggerRepository);

        public static Type BodyType => typeof(BloggerUser);

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

            InitializeGhost(_record.InitialGhost, (BloggerUser_VectorTable*)_record.Initial);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
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
            vt->Std.TypeCombo = new GhostId(GhostIdKind.Entity, (ushort)TypeIdentifier, default, default).TypeCombo;
            vt->Std.ModelVersion = (short)SourceVersion;
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
            h->Id = GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier);

            var emptyString = new ArrayMapSmallEntry()
            {
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

        #region INITIAL
        public static BloggerUser_VectorTable* GetInitial()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Std.SwapAnyArray = &Initial_SwapAnyArray;
            vt->Active_Setter = &Initial_Active_Setter;
            vt->CustomerCode_Setter = &Initial_CustomerCode_Setter;
            vt->BirthDate_Setter = &Initial_BirthDate_Setter;
            return vt;
        }

        public static BloggerUser InitialToStandalone(BloggerUser body)
        {
            var union = Unsafe.As<BodyUnion>(body);
            union._data = TransientGhostMemoryAllocator.Allocate(union._data.Length);
            union._vTablePtr = (nint)_record.Standalone;
            _record.InitialGhost.CopyTo(union._data);
            union._data.Set<GhostId>(0, GhostId.NewId(GhostIdKind.Entity, (ushort)TypeIdentifier));
            return body;
        }

        public static unsafe void Initial_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<BloggerUser>(body)).SwapAnyArray(src, arrayIndex);

        public static unsafe void Initial_Active_Setter(BloggerUser body, bool value)
            => Standalone_Active_Setter(InitialToStandalone(body), value);

        public static unsafe void Initial_CustomerCode_Setter(BloggerUser body, int value)
            => Standalone_CustomerCode_Setter(InitialToStandalone(body), value);

        public static unsafe void Initial_BirthDate_Setter(BloggerUser body, DateTime value)
            => Standalone_BirthDate_Setter(InitialToStandalone(body), value);
        #endregion

        #region STANDALONE
        public static BloggerUser_VectorTable* GetStandalone()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Std.SwapAnyArray = &Standalone_SwapAnyArray;
            vt->Active_Setter = &Standalone_Active_Setter;
            vt->CustomerCode_Setter = &Standalone_CustomerCode_Setter;
            vt->BirthDate_Setter = &Standalone_BirthDate_Setter;
            return vt;
        }

        public static unsafe void Standalone_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<BloggerUser>(body).SwapAnyArray(src, arrayIndex);

        public static unsafe void Standalone_Active_Setter(BloggerUser body, bool value)
            => Unsafe.As<BodyUnion>(body)._data.Set<bool>(body._vTable->Active_FieldOffset, value);

        public static unsafe void Standalone_CustomerCode_Setter(BloggerUser body, int value)
            => Unsafe.As<BodyUnion>(body)._data.Set<int>(body._vTable->CustomerCode_FieldOffset, value);

        public static unsafe void Standalone_BirthDate_Setter(BloggerUser body, DateTime value)
            => Unsafe.As<BodyUnion>(body)._data.Set<DateTime>(body._vTable->BirthDate_FieldOffset, value);
        #endregion
    }
}
