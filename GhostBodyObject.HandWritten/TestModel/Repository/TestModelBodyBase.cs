using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.Entities.Repository
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 42)]
    public abstract class TestModelBodyBase : BodyBase
    {
        [FieldOffset(0)]
        protected TestModelTransaction _ownerTransaction;

        public TestModelTransaction Transaction => _ownerTransaction;

        protected TestModelBodyBase()
        {
            var current = TestModelContext.Transaction;
            if (current == null)
                throw new InvalidOperationException("Must be created in a transaction."); // Option B: _ownerToken = new GhostContext();
            else
                _ownerTransaction = current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected TestModelGhostWriteLock GuardWriteScope()
        {
            var current = TestModelContext.Transaction;
            if (_ownerTransaction != current)
                ThrowContextMismatch();
            return new TestModelGhostWriteLock(_ownerTransaction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void GuardLocalScope()
        {
            if (_ownerTransaction != TestModelContext.Transaction)
                ThrowContextMismatch();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowContextMismatch() => throw new InvalidOperationException("Cross-Context Violation.");
    }
}
