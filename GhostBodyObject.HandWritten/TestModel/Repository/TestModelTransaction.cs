using GhostBodyObject.Repository.Repository.Transaction;

namespace GhostBodyObject.HandWritten.TestModel.Repository
{
    public class TestModelTransaction : RepositoryTransaction
    {
        public TestModelRepository Repository { get; }

        public void Commit()
        {

        }

        public void Rollback()
        {

        }

        public void Close()
        {

        }


        public TestModelTransaction(TestModelRepository repository, bool readOnly = false) : base(repository, readOnly)
        {
            Repository = repository;
        }
    }
}
