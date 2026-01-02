using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Repository.Transaction;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    // ---------------------------------------------------------
    // 3. The Base Entity
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
    public abstract class BloggerBodyBase : IEntityBody
    {
        [FieldOffset(0)]
        protected BloggerTransaction _ownerTransaction;

        [FieldOffset(8)]
        protected IntPtr _vTablePtr;

        [FieldOffset(16)]
        protected PinnedMemory<byte> _data;

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
