using GhostBodyObject.Repository;
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

        public int LastName_MapEntryOffset;

        public int Pseudonyme_MapEntryOffset;

        public int Presentation_MapEntryOffset;

        public int City_MapEntryOffset;

        public int Country_MapEntryOffset;

        public int CompanyName_MapEntryOffset;

        public int Address1_MapEntryOffset;

        public int Address2_MapEntryOffset;

        public int Address3_MapEntryOffset;

        public int ZipCode_MapEntryOffset;

        public int Hobbies_MapEntryOffset;

        // -------- Indexes for quick access
        public int FirstName_MapEntryIndex;

        public int LastName_MapEntryIndex;

        public int Pseudonyme_MapEntryIndex;

        public int Presentation_MapEntryIndex;

        public int City_MapEntryIndex;

        public int Country_MapEntryIndex;

        public int CompanyName_MapEntryIndex;

        public int Address1_MapEntryIndex;

        public int Address2_MapEntryIndex;

        public int Address3_MapEntryIndex;

        public int ZipCode_MapEntryIndex;

        public int Hobbies_MapEntryIndex;


        // ---------------------------------------------------------
        // Setters
        // ---------------------------------------------------------

        public delegate*<BloggerUser, bool, void> Active_Setter;

        public delegate*<BloggerUser, int, void> CustomerCode_Setter;

        public delegate*<BloggerUser, DateTime, void> BirthDate_Setter;

        // -------- It is needed to have all properties with a setter : for indexing purposes + triggers !
        public delegate*<BloggerUser, GhostStringUtf16, void> FirstName_Setter;
    }
}
