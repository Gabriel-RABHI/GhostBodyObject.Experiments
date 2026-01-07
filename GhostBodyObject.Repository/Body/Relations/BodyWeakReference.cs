using GhostBodyObject.Repository.Body.Contracts;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Body.Relations
{
    public ref struct BodyWeakReference<TBody>
        where TBody : BodyBase
    {
        public bool IsAlive => throw new NotImplementedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TBody(BodyWeakReference<TBody> value) => throw new NotImplementedException();
    }
}
