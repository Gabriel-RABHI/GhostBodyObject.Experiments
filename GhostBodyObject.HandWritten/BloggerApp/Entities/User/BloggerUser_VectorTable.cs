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
        public int BirthDate_FieldOffset;

        public int CustomerCode_FieldOffset;

        public int Active_FieldOffset;

        // ---------------------------------------------------------
        // Array Map Offsets
        // ---------------------------------------------------------
        public int FirstName_MapEntryOffset;

        public int CustomerCodeTiers_MapEntryOffset;

        // -------- Indexes for quick access
        public int First_MapEntryIndex;

        public int CustomerCodeTiers_MapEntryIndex;


        // ---------------------------------------------------------
        // Setters
        // ---------------------------------------------------------

        public delegate*<BloggerUser, bool, void> Active_Setter;

        public delegate*<BloggerUser, int, void> CustomerCode_Setter;

        public delegate*<BloggerUser, DateTime, void> BirthDate_Setter;
    }
}
