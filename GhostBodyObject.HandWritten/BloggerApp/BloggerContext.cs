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

        private static IBloggerScope NewContext(BloggerRepository repository, bool readOnly = false)
        {
            if (FastCache != null)
            {
                throw new InvalidOperationException("Cannot nest contexts.");
            }
            var newToken = new BloggerTransaction(repository, readOnly);
            _currentToken.Value = newToken;
            return new BloggerContextScope(newToken);
        }

        public static IBloggerScope NewWriteContext(BloggerRepository repository) => NewContext(repository, false);

        public static IBloggerScope NewReadContext(BloggerRepository repository) => NewContext(repository, true);

        private class BloggerContextScope : IBloggerScope
        {
            private readonly BloggerTransaction _transaction;
            private readonly BloggerRepository _repository;
            private bool _disposed;

            public BloggerTransaction Transaction => _transaction;

            public BloggerRepository Repository => _repository;

            public BloggerContextScope(BloggerTransaction transaction)
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
