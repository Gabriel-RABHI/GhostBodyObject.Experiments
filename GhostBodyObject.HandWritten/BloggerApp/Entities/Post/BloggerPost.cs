using GhostBodyObject.HandWritten.Blogger.Repository;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.Post
{
    public sealed class BloggerPost : BloggerBodyBase
    {
        private string _name;
        private int _age;

        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // The Guard now acts as a Scope
#if DEBUG
            GuardLocalScope();
#endif
                return _name;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                // The Guard now acts as a Scope
#if DEBUG
            using (GuardWriteScope())
            {
#endif
                _name = value;
#if DEBUG
            }
#endif
            }
        }

        public int Age
        {
            get => _age;
            set
            {
#if DEBUG
            using (GuardWriteScope())
            {
#endif
                _age = value;
#if DEBUG
            }
#endif
            }
        }
    }
}
