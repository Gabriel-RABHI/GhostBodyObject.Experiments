using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    // ---------------------------------------------------------
    // 3. The Base Entity
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 42)]
    public abstract class BloggerBodyBase : BodyBase
    {
        [FieldOffset(0)]
        protected BloggerTransaction _ownerTransaction;

        public BloggerTransaction Transaction => _ownerTransaction;

        protected BloggerBodyBase()
        {
            var current = BloggerContext.Transaction;
            if (current == null)
                throw new InvalidOperationException("Must be created in a transaction."); // Option B: _ownerToken = new GhostContext();
            else
            {
                if (BloggerContext.Transaction.IsReadOnly)
                    throw new InvalidOperationException("Cannot create new body in a read-only transaction.");
                _ownerTransaction = current;
            }
        }

        protected BloggerBodyBase(PinnedMemory<byte> ghost)
        {
            var current = BloggerContext.Transaction;
            if (current == null)
                throw new InvalidOperationException("Must be created in a transaction."); // Option B: _ownerToken = new GhostContext();
            else
            {
                _ownerTransaction = current;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected BloggerGhostWriteLock GuardWriteScope()
        {
            var current = BloggerContext.Transaction;
            if (_ownerTransaction != current)
                ThrowContextMismatch();
            CheckTransactionStatus();
            return new BloggerGhostWriteLock(_ownerTransaction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void GuardLocalScope()
        {
            if (_ownerTransaction != BloggerContext.Transaction)
                ThrowContextMismatch();
            CheckTransactionStatus();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowContextMismatch() => throw new InvalidOperationException("Cross-Context Violation.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckTransactionStatus()
        {
            if (_ownerTransaction.NeedReborn)
            {
                // It the transaction is closed :
                //      - It is not readonly (write transaction)
                //      - It is readonly (read transaction) but not the "common read" transaction
                // -> throw a ClosedTransactionException
                // 
                // Otherwise, renew the body in the current "common read" transaction :
                // - Retreive the current transaction
                throw new NotImplementedException();
            }
        }
    }
}
