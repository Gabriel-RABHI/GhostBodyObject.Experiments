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

        public static void Commit(bool concurrently = false) => FastCache.Commit(concurrently);

        public static void Rollback() => FastCache.Rollback();

        private class BloggerContextScope : IBloggerScope
        {
            private readonly BloggerTransaction _transaction;
            private bool _disposed;

            public BloggerContextScope(BloggerTransaction transaction)
            {
                _transaction = transaction;
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
