using GhostBodyObject.HandWritten.Entities.Contracts;
using GhostBodyObject.HandWritten.Entities.Repository;

namespace GhostBodyObject.HandWritten.Entities
{
    public static class TestModelContext
    {
        private static readonly AsyncLocal<TestModelTransaction?> _currentToken = new AsyncLocal<TestModelTransaction?>(OnContextChanged);

        [ThreadStatic]
        private static TestModelTransaction? FastCache;

        public static TestModelTransaction? Transaction => FastCache;

        private static void OnContextChanged(AsyncLocalValueChangedArgs<TestModelTransaction?> args)
            => FastCache = args.CurrentValue;

        public static ITestModelScope OpenReadContext(TestModelRepository repository, bool readOnly = false)
        {
            if (FastCache != null)
            {
                throw new InvalidOperationException("Cannot nest contexts.");
            }
            var newToken = new TestModelTransaction(repository);
            _currentToken.Value = newToken;
            return new TestModelContextScope(newToken);
        }

        private class TestModelContextScope : ITestModelScope
        {
            private readonly TestModelTransaction _transaction;
            private readonly TestModelRepository _repository;
            private bool _disposed;

            public TestModelTransaction Transaction => _transaction;

            public TestModelRepository Repository => _repository;

            public TestModelContextScope(TestModelTransaction transaction)
            {
                _transaction = transaction;
                _repository = transaction.Repository;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                if (_currentToken.Value == _transaction)
                {
                    _currentToken.Value = null;
                    _transaction.Close();
                }
            }
        }
    }
}
