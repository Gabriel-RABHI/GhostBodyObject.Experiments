using GhostBodyObject.Common.Memory;
using GhostBodyObject.Experiments.BabyBody;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{

    // ---------------------------------------------------------
    // 4. The Customer Entity (User Code)
    // ---------------------------------------------------------
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
    public class BloggerUser : BloggerBodyBase
    {
        public int ModelVersion => 1;

        internal unsafe BloggerUser_VectorTable* _vTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BloggerUser_VectorTable*)_vTablePtr;

            set => _vTablePtr = (IntPtr)value;
        }

        public BloggerUser()
        {
            unsafe
            {
                VectorTableRegistry<BloggerRepository, BloggerUser>.BuildInitialVersion(ModelVersion, this);
            }
        }

        public BloggerUser(PinnedMemory<byte> ghost, bool mapped = true)
        {
            unsafe
            {
                if (mapped)
                    VectorTableRegistry<BloggerRepository, BloggerUser>.BuildMappedVersion(ghost, this, Transaction.IsReadOnly);
                else
                    VectorTableRegistry<BloggerRepository, BloggerUser>.BuildStandaloneVersion(ghost, this);
            }
        }

        public unsafe bool Active
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                return _data.Get<bool>(_vTable->Active_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                using (GuardWriteScope())
                {
                    _vTable->Active_Setter(this, value);
                }
            }
        }

        public GhostString CustomerName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                unsafe
                {
                    var stringOffset = _data.Get<ArrayMapSmallEntry>(_vTable->CustomerName_MapEntryOffset);
                    return new GhostString(this, _vTable->CustomerName_MapEntryIndex, _data.Slice((int)stringOffset.ArrayOffset, (int)stringOffset.ArrayLength));
                }
            }
        }
    }
}
