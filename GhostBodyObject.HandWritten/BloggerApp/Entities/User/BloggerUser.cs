using GhostBodyObject.Common.Memory;
using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
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

        public GhostStringUtf16 FirstName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                unsafe
                {
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->FirstName_MapEntryOffset);
                    // PhysicalSize = ArrayLength * ValueSize (in bytes)
                    return new GhostStringUtf16(this, _vTable->First_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                using (GuardWriteScope())
                {
                    unsafe
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->First_MapEntryIndex);
                    }
                }
            }
        }

        
        public GhostStringUtf16 LastName
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Pseudonyme
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Presentation
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 City
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Country
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 CompanyName
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Address1
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Address2
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Address3
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 ZipCode
        {
            get => new GhostStringUtf16(); set;
        }

        public GhostStringUtf16 Hobbies
        {
            get => new GhostStringUtf16(); set;
        }
        

        public string FirstNameString
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                //GuardLocalScope();
                unsafe
                {
                    var stringOffset = _data.Get<ArrayMapSmallEntry>(_vTable->FirstName_MapEntryOffset);
                    return new string((char*)this._data.Ptr, stringOffset.ArrayOffset, (int)stringOffset.ArrayLength / 2);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                //using (GuardWriteScope())
                {
                    unsafe
                    {
                        var union = Unsafe.As<BodyUnion>(this);
                        _vTable->Std.SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _vTable->First_MapEntryIndex);
                    }
                }
            }
        }
    }
}
