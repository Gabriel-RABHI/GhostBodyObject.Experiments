using GhostBodyObject.Repository.Body.Vectors;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{
    public unsafe struct BloggerUser_VectorTable
    {
        // ---------------------------------------------------------
        // Standard Fields
        // ---------------------------------------------------------
        public VectorTableHeader Std;

        // ---------------------------------------------------------
        // Value Field Offsets
        // ---------------------------------------------------------
        public int CreatedOn_FieldOffset;

        public int CustomerCode_FieldOffset;

        public int Active_FieldOffset;

        // ---------------------------------------------------------
        // Array Map Offsets
        // ---------------------------------------------------------
        public int CustomerName_MapEntryOffset;

        public int CustomerCodeTiers_MapEntryOffset;

        public int CustomerName_MapEntryIndex;

        public int CustomerCodeTiers_MapEntryIndex;


        // ---------------------------------------------------------
        // Setters
        // ---------------------------------------------------------
        public delegate*<BloggerUser, Memory<byte>, int, void> SwapAnyArray;

        public delegate*<BloggerUser, bool, void> Active_Setter;

        public delegate*<BloggerUser, int, void> CustomerCode_Setter;

        public delegate*<BloggerUser, DateTime, void> CreatedOn_Setter;
    }
}
