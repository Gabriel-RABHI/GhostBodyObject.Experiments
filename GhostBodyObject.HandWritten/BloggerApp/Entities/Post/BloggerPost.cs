using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Contracts;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.Post
{
    public sealed class BloggerPost : BloggerBodyBase, IHasTypeIdentifier, IBodyFactory<BloggerPost>
    {
        public static GhostTypeCombo GetTypeIdentifier() => new GhostTypeCombo(GhostIdKind.Entity, 101);

        public static BloggerPost Create(PinnedMemory<byte> ghost, bool mapped = true, bool register = true)
            => new BloggerPost(ghost, mapped, register);

        private string _name;
        private int _age;

        public BloggerPost(PinnedMemory<byte> ghost, bool mapped = true, bool register = true)
            : base(ghost)
        {
        }


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
