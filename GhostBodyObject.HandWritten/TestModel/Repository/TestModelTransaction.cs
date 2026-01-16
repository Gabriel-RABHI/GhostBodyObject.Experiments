using GhostBodyObject.HandWritten.Entities.Arrays;
using GhostBodyObject.Repository.Repository.Transaction;
using GhostBodyObject.Repository.Repository.Transaction.Collections;
using GhostBodyObject.Repository.Repository.Transaction.Index;

namespace GhostBodyObject.HandWritten.Entities.Repository
{
    public class TestModelTransaction : RepositoryTransactionBase
    {
        public TestModelRepository Repository { get; }

        public TestModelTransaction(TestModelRepository repository, bool readOnly = false) : base(repository, readOnly, 4096)
        {
            Repository = repository;

            _arraysAsStringsAndSpansLargeMap = new ShardedTransactionBodyMap<ArraysAsStringsAndSpansLarge>();
            _arraysAsStringsAndSpansSmallMap = new ShardedTransactionBodyMap<ArraysAsStringsAndSpansSmall>();
        }

        public void Commit()
        {

        }

        public void Rollback()
        {

        }

        public void Close()
        {

        }

        #region
        private ShardedTransactionBodyMap<ArraysAsStringsAndSpansLarge> _arraysAsStringsAndSpansLargeMap;

        public void RegisterBody(ArraysAsStringsAndSpansLarge body)
            => _arraysAsStringsAndSpansLargeMap.Set(body);

        //public BodyCollection<ArraysAsStringsAndSpansLarge> ArraysAsStringsAndSpansLargeCollection
        //    => new BodyCollection<ArraysAsStringsAndSpansLarge>(_arraysAsStringsAndSpansLargeMap);
        #endregion

        #region
        private ShardedTransactionBodyMap<ArraysAsStringsAndSpansSmall> _arraysAsStringsAndSpansSmallMap;

        public void RegisterBody(ArraysAsStringsAndSpansSmall body)
            => _arraysAsStringsAndSpansSmallMap.Set(body);

        //public BodyCollection<ArraysAsStringsAndSpansSmall> ArraysAsStringsAndSpansSmallCollection
        //    => new BodyCollection<ArraysAsStringsAndSpansSmall>(_arraysAsStringsAndSpansSmallMap);
        #endregion
    }
}
