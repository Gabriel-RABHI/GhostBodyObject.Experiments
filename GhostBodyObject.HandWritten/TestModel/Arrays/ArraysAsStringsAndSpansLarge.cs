using GhostBodyObject.HandWritten.BloggerApp.Entities.User;
using GhostBodyObject.HandWritten.TestModel.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Values;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.TestModel.Arrays
{
    public class ArraysAsStringsAndSpansLarge : TestModelBodyBase
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


    public unsafe static class ArraysAsStringsAndSpansLarge_V1_V1_MappingVectorTableBuilder
    {
        private static bool _initialized = false;
        private static VectorTableRecord _record;

        public static Type RepositoryType => typeof(TestModelRepository);

        public static Type BodyType => typeof(ArraysAsStringsAndSpansLarge);

        public static int TypeIdentifier => 10;

        public static int SourceVersion => 1;

        public static int TargetVersion => 1;

        public static VectorTableRecord GetTableRecord()
        {
            throw new NotImplementedException();
        }

        #region COMMON
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetCommon()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region INITIAL
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetInitial()
        {
            throw new NotImplementedException();
        }

        public static ArraysAsStringsAndSpansLarge InitialToStandalone(ArraysAsStringsAndSpansLarge body)
        {
            throw new NotImplementedException();
        }

        public static unsafe void Initial_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => InitialToStandalone(Unsafe.As<ArraysAsStringsAndSpansLarge>(body)).SwapAnyArray(src, arrayIndex);
        #endregion

        #region STANDALONE
        public static ArraysAsStringsAndSpansLarge_VectorTable* GetStandalone()
        {
            throw new NotImplementedException();
        }

        public static unsafe void Standalone_SwapAnyArray(BodyUnion body, ReadOnlySpan<byte> src, int arrayIndex)
            => Unsafe.As<ArraysAsStringsAndSpansLarge>(body).SwapAnyArray(src, arrayIndex);
        #endregion
    }
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
        public delegate*<BloggerUser, bool, void> OneDateTime_Setter;

        public delegate*<BloggerUser, int, void> OneInt_Setter;
    }
}
