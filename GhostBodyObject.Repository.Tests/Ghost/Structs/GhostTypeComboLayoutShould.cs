using global::GhostBodyObject.Repository.Ghost.Constants;
using global::GhostBodyObject.Repository.Ghost.Structs;

namespace GhostBodyObject.Common.Tests.Objects // Use the same namespace as GhostIdShould to be safe, or a sub-namespace
{
    public class GhostTypeComboLayoutShould
    {
        [Fact]
        public void Store_Kind_In_Lower_3_Bits()
        {
            // Kind = 7 (111 binary)
            // Type = 0
            // Expected Value: ...0000000000111 = 7 (0x07)

            var combo = new GhostTypeCombo(GhostIdKind.Other, 0);

            Assert.Equal((ushort)0x07, combo.Value);
            Assert.Equal(GhostIdKind.Other, combo.Kind);
            Assert.Equal(0, combo.TypeIdentifier);
        }

        [Fact]
        public void Store_TypeIdentifier_shifted_By_3_Bits()
        {
            // Kind = 0
            // Type = 1
            // Expected: ...00001000 = 8 (0x08)

            var combo = new GhostTypeCombo(GhostIdKind.Entity, 1);

            Assert.Equal((ushort)0x08, combo.Value);
            Assert.Equal(GhostIdKind.Entity, combo.Kind);
            Assert.Equal(1, combo.TypeIdentifier);
        }

        [Fact]
        public void Combine_Kind_And_Type_Correctly()
        {
            // Kind = 3 (011) (Edge)
            // Type = 5 (101)
            // Expected: 
            // Type << 3 = 101000 (40)
            // Kind      =    011 (3)
            // Result    = 101011 (43) (0x2B)

            var combo = new GhostTypeCombo(GhostIdKind.Edge, 5);

            Assert.Equal((ushort)43, combo.Value);
            Assert.Equal(GhostIdKind.Edge, combo.Kind);
            Assert.Equal(5, combo.TypeIdentifier);
        }

        [Fact]
        public void Handle_Max_Values()
        {
            // Kind = 7 (111)
            // Type = 8191 (1111111111111) (Max 13 bits)
            // Expected: All ones = 0xFFFF

            var combo = new GhostTypeCombo(GhostIdKind.Other, 8191);

            Assert.Equal(ushort.MaxValue, combo.Value);
            Assert.Equal(GhostIdKind.Other, combo.Kind);
            Assert.Equal(8191, combo.TypeIdentifier);
        }
    }
}
