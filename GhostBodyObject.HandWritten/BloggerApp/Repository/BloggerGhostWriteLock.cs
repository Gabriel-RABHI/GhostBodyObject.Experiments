using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.Blogger.Repository
{
    // ---------------------------------------------------------
    // 2. The Safety Mechanism (The "Guard")
    // ---------------------------------------------------------
    public readonly struct BloggerGhostWriteLock : IDisposable
    {
        private readonly BloggerTransaction? _token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BloggerGhostWriteLock(BloggerTransaction token)
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
