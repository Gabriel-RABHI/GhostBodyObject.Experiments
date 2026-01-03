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
                //GuardLocalScope();
                unsafe
                {
                    var stringOffset = _data.Get<ArrayMapSmallEntry>(_vTable->FirstName_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->First_MapEntryIndex, _data.Slice((int)stringOffset.ArrayOffset, (int)stringOffset.ArrayLength));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                //using (GuardWriteScope())
                {
                    unsafe
                    {
                        // Use the source span directly - this is safe because:
                        // 1. If value came from a string conversion, AsSpan() returns the original string's span
                        // 2. If value came from another GhostStringUtf16, AsSpan() returns the pinned memory span
                        // 3. SwapAnyArray copies the data immediately, so no pointer escapes this scope
                        var union = Unsafe.As<BodyUnion>(this);
                        _vTable->Std.SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _vTable->First_MapEntryIndex);
                    }
                }
            }
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
                        // Use the source span directly - this is safe because:
                        // 1. If value came from a string conversion, AsSpan() returns the original string's span
                        // 2. If value came from another GhostStringUtf16, AsSpan() returns the pinned memory span
                        // 3. SwapAnyArray copies the data immediately, so no pointer escapes this scope
                        var union = Unsafe.As<BodyUnion>(this);
                        _vTable->Std.SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _vTable->First_MapEntryIndex);
                    }
                }
            }
        }
    }
}
