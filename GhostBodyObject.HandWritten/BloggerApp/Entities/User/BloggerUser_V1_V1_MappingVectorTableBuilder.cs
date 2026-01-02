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
        public static Type RepositoryType => typeof(BloggerRepository);

        public static Type BodyType => typeof(BloggerUser);

        public static int SourceVersion => 1;

        public static int TargetVersion => 1;

        public static VectorTableRecord GetTableRecord()
        {
            var record = new VectorTableRecord();
            record.Standalone = (VectorTableHeader*)GetStandalone();
            record.GhostSize = record.Standalone->MinimalGhostSize;

            var buff = GC.AllocateArray<byte>(record.GhostSize, true);
            record.InitialGhost = new PinnedMemory<byte>(buff, 0, buff.Length);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
            return record;
        }

        #region COMMON
        public static BloggerUser_VectorTable* GetCommon()
        {
            var vt = (BloggerUser_VectorTable*)NativeMemory.Alloc((nuint)sizeof(BloggerUser_VectorTable));
            var f = new GhostHeaderIncrementor();
            vt->CreatedOn_FieldOffset = f.Push<DateTime>();      // CreatedOn : 40
            vt->CustomerCode_FieldOffset = f.Push<int>();        // CustomerCode : 48
            vt->Active_FieldOffset = f.Push<bool>();             // Active : 49
            f.Padd(4); // -> 52
            vt->CustomerName_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            vt->CustomerCodeTiers_MapEntryOffset = f.Push<ArrayMapSmallEntry>();

            vt->Std.MinimalGhostSize = f.Offset;
            return vt;
        }
        #endregion

        #region STANDALONE
        public static BloggerUser_VectorTable* GetInitial()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Active_Setter = &Initial_Active_Setter;
            vt->CustomerCode_Setter = &Initial_CustomerCode_Setter;
            vt->CreatedOn_Setter = &Initial_CreatedOn_Setter;
            return vt;
        }

        public static unsafe void Initial_Active_Setter(BloggerUser body, bool value)
        {
            Unsafe.As<BodyUnion>(body)._data.Set<bool>(body._vTable->Active_FieldOffset, value);
        }

        public static unsafe void Initial_CustomerCode_Setter(BloggerUser body, int value)
        {
        }

        public static unsafe void Initial_CreatedOn_Setter(BloggerUser body, DateTime value)
        {
        }
        #endregion

        #region STANDALONE
        public static BloggerUser_VectorTable* GetStandalone()
        {
            var vt = GetCommon();

            // -------- Function Pointers -------- //
            vt->Active_Setter = &Standalone_Active_Setter;
            vt->CustomerCode_Setter = &Standalone_CustomerCode_Setter;
            vt->CreatedOn_Setter = &Standalone_CreatedOn_Setter;
            return vt;
        }

        public static unsafe void Standalone_Active_Setter(BloggerUser body, bool value)
        {
            Unsafe.As<BodyUnion>(body)._data.Set<bool>(body._vTable->Active_FieldOffset, value);
        }

        public static unsafe void Standalone_CustomerCode_Setter(BloggerUser body, int value)
        {
        }

        public static unsafe void Standalone_CreatedOn_Setter(BloggerUser body, DateTime value)
        {
        }
        #endregion
    }
}
