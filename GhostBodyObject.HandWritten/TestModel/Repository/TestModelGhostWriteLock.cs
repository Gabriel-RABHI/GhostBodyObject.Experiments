using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.Entities.Repository
{
    public readonly struct TestModelGhostWriteLock : IDisposable
    {
        private readonly TestModelTransaction? _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TestModelGhostWriteLock(TestModelTransaction token)
        {
            _token = token;
            if (_token.IsBusy)
                throw new InvalidOperationException("Parallelism detected! A concurrent thread is already modifying data in this context.");
            _token.IsBusy = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _token.IsBusy = false;
    }
}
