using GhostBodyObject.HandWritten.Blogger.Repository;
using GhostBodyObject.Repository;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Body.Vectors;
using GhostBodyObject.Repository.Ghost.Structs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.UserFlat
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
    public sealed class BloggerUserFlat : BloggerBodyBase
    {
        public int ModelVersion => 1;

        private unsafe BloggerUserFlat_VectorTable* _vTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (BloggerUserFlat_VectorTable*)_vTablePtr;

            set => _vTablePtr = (IntPtr)value;
        }

        public BloggerUserFlat()
        {
            unsafe
            {
                VectorTableRegistry<BloggerRepository, BloggerUserFlat>.BuildFlatStandaloneVersion(ModelVersion, this);
            }
        }

        public BloggerUserFlat(PinnedMemory<byte> ghost, bool mapped = true)
        {
            unsafe
            {
                if (mapped)
                {
                    VectorTableRegistry<BloggerRepository, BloggerUserFlat>.BuildMappedVersion(ghost, this, Transaction.IsReadOnly);
                    _mapped = true;
                }
                else
                {
                    VectorTableRegistry<BloggerRepository, BloggerUserFlat>.BuildStandaloneVersion(ghost, this);
                    _mapped = false;
                }
                _immutable = Transaction.IsReadOnly;
            }
        }
        

        public void ToStandalone()
        {
            _mapped = false;
        }

        public unsafe DateTime BirthDate

        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_immutable)
                    GuardLocalScope();
                return _data.Get<DateTime>(_vTable->BirthDate_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_immutable)
                    throw new InvalidOperationException("Cannot modify an immutable Body object.");
                using (GuardWriteScope())
                {
                    if (_mapped)
                        ToStandalone();
                    _data.Set<DateTime>(_vTable->BirthDate_FieldOffset, value);
                }
            }
        }

        public unsafe int CustomerCode

        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!_immutable)
                    GuardLocalScope();
                return _data.Get<int>(_vTable->CustomerCode_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (_immutable)
                    throw new InvalidOperationException("Cannot modify an immutable Body object.");
                using (GuardWriteScope())
                {
                    if (_mapped)
                        ToStandalone();
                    _data.Set<int>(_vTable->CustomerCode_FieldOffset, value);
                }
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
                if (_immutable)
                    throw new InvalidOperationException("Cannot modify an immutable Body object.");
                using (GuardWriteScope())
                {
                    if (_mapped)
                        ToStandalone();
                    if (_vTable->Active_FieldOffset > 0)
                        _data.Set<bool>(_vTable->Active_FieldOffset, value);
                }
            }
        }

        public GhostStringUtf16 FirstName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->FirstName_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->FirstName_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    if (_immutable)
                        throw new InvalidOperationException("Cannot modify an immutable Body object.");
                    using (GuardWriteScope())
                    {
                        if (_mapped)
                            ToStandalone();
                        SwapAnyArray(MemoryMarshal.AsBytes(value.AsSpan()), _vTable->FirstName_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 LastName
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Pseudonyme
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Presentation
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 City
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Country
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 CompanyName
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Address1
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Address2
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Address3
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 ZipCode
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }

        public GhostStringUtf16 Hobbies
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }
    }
}
