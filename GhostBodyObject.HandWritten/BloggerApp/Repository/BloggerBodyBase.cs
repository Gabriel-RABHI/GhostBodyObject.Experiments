using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    // ---------------------------------------------------------
    // 3. The Base Entity
    // ---------------------------------------------------------
    public abstract class BloggerBodyBase
    {
        private readonly BloggerTransaction _ownerTransaction;

        public BloggerTransaction Transaction => _ownerTransaction;

        protected BloggerBodyBase()
        {
            var current = BloggerContext.Transaction;
            if (current == null)
                throw new InvalidOperationException("Must be created in a transaction."); // Option B: _ownerToken = new GhostContext();
            else
                _ownerTransaction = current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected BloggerGhostWriteLock GuardWriteScope()
        {
            var current = BloggerContext.Transaction;
            if (_ownerTransaction != current)
                ThrowContextMismatch();
            return new BloggerGhostWriteLock(_ownerTransaction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void GuardLocalScope()
        {
            if (_ownerTransaction != BloggerContext.Transaction)
                ThrowContextMismatch();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowContextMismatch() => throw new InvalidOperationException("Cross-Context Violation.");
    }
}
