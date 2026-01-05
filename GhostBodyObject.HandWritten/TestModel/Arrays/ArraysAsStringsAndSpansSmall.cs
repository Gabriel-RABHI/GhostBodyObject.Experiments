using GhostBodyObject.Common.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Blogger;
using GhostBodyObject.HandWritten.Blogger.Contracts;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.HandWritten.BloggerApp.Entities.Post;
using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.TestModel.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Ghost.Values;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.HandWritten.TestModel.Arrays
{
    public class ArraysAsStringsAndSpansSmall : TestModelBodyBase
    {
        public DateTime OneDateTime
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public int OneInt
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public GhostSpan<Guid> Guids
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public GhostSpan<DateTime> DateTimes
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public GhostStringUtf16 StringU16
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public GhostStringUtf8 StringU8
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }


    public unsafe static class ArraysAsStringsAndSpansSmall_V1_V1_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(TestModelRepository);

        public static Type BodyType => typeof(ArraysAsStringsAndSpansSmall);

        public static int TypeIdentifier => 10;

        public static int SourceVersion => 1;

        public static int TargetVersion => 1;

        public static VectorTableRecord GetTableRecord()
        {
            throw new NotImplementedException();
        }

        #region COMMON
        public static ArraysAsStringsAndSpansSmall_VectorTable* GetCommon()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region INITIAL
        public static ArraysAsStringsAndSpansSmall_VectorTable* GetInitial()
        {
            throw new NotImplementedException();
        }

        public static ArraysAsStringsAndSpansSmall InitialToStandalone(ArraysAsStringsAndSpansSmall body)
        {
            throw new NotImplementedException();
        }

        public static unsafe void Initial_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansSmall>(body)).SwapAnyArray(src, arrayIndex);
        #endregion

        #region STANDALONE
        public static ArraysAsStringsAndSpansSmall_VectorTable* GetStandalone()
        {
            throw new NotImplementedException();
        }

        public static unsafe void Standalone_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<ArraysAsStringsAndSpansSmall>(body).SwapAnyArray(src, arrayIndex);
        #endregion
    }
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
        // Setters
        // ---------------------------------------------------------
        public delegate*<BloggerUser, bool, void> OneDateTime_Setter;

        public delegate*<BloggerUser, int, void> OneInt_Setter;
    }
}
