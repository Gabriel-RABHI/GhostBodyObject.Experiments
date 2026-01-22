using GhostBodyObject.HandWritten.Entities;
using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.HandWritten.Entities.Repository;

namespace GhostBodyObject.HandWritten.Tests.TestModel
{
    public class TestModelContextShould
    {
        [Fact]
        public void RegisterOneBody()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                var body = new ArraysAsStringsAndSpansLarge();
                //Assert.Equal(1, TestModelContext.Transaction.ArraysAsStringsAndSpansLargeCollection.Count);
            }
        }

        [Fact]
        public void RegisterTwoBodyTypes()
        {
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {

                var small = new ArraysAsStringsAndSpansSmall();
                //Assert.Equal(1, TestModelContext.Transaction.ArraysAsStringsAndSpansSmallCollection.Count);

                var large = new ArraysAsStringsAndSpansLarge();
                //Assert.Equal(1, TestModelContext.Transaction.ArraysAsStringsAndSpansLargeCollection.Count);
            }
        }

        [Fact]
        public void RegisterASet()
        {
#if RELEASE
            var count = 100_000;
#else
            var count = 10_000;
#endif
            var repository = new TestModelRepository();
            using (TestModelContext.OpenReadContext(repository))
            {
                for (int i = 0; i < count; i++)
                {
                    var body = new ArraysAsStringsAndSpansLarge();
                    body.StringU16 = "Test " + i;
                }
                //Assert.Equal(count, TestModelContext.Transaction.ArraysAsStringsAndSpansLargeCollection.Count);
                TestModelContext.Transaction.Commit();
            }
        }
    }
}
