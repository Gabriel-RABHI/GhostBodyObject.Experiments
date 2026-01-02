using GhostBodyObject.HandWritten.Blogger.Contracts;
using GhostBodyObject.HandWritten.Blogger.Repository;

namespace GhostBodyObject.HandWritten.Blogger
{
    public static class BloggerContext
    {
        private static readonly AsyncLocal<BloggerTransaction?> _currentToken = new AsyncLocal<BloggerTransaction?>(OnContextChanged);

        [ThreadStatic]
        private static BloggerTransaction? FastCache;

        public static BloggerTransaction? Transaction => FastCache;

        private static void OnContextChanged(AsyncLocalValueChangedArgs<BloggerTransaction?> args)
            => FastCache = args.CurrentValue;

        public static IBloggerScope OpenReadContext(BloggerRepository repository, bool readOnly = false)
        {
            var parent = FastCache;
            var newToken = new BloggerTransaction(repository, parent);
            _currentToken.Value = newToken;
            return new BloggerContextScope(newToken, parent);
        }

        private class BloggerContextScope : IBloggerScope
        {
            private readonly BloggerTransaction _transaction;
            private readonly BloggerTransaction? _parentTransaction;
            private readonly BloggerRepository _repository;
            private bool _disposed;

            public BloggerTransaction Transaction => _transaction;

            public BloggerRepository Repository => _repository;

            public BloggerContextScope(BloggerTransaction transaction, BloggerTransaction? parentTransaction)
            {
                _transaction = transaction;
                _parentTransaction = parentTransaction;
                if (_parentTransaction != null && _parentTransaction.Repository != _transaction.Repository)
                    throw new InvalidOperationException("Nested transactions must belong to the same repository.");
                _repository = transaction.Repository;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                if (_currentToken.Value == _transaction)
                {
                    _currentToken.Value = _parentTransaction;
                    FastCache = _parentTransaction;
                    _transaction.Close();
                }
            }
        }
    }
}
