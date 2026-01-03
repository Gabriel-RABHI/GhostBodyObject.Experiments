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
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
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

        public unsafe DateTime BirthDate

        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                return _data.Get<DateTime>(_vTable->BirthDate_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                using (GuardWriteScope())
                {
                    _vTable->BirthDate_Setter(this, value);
                }
            }
        }

        public unsafe int CustomerCode

        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                GuardLocalScope();
                return _data.Get<int>(_vTable->CustomerCode_FieldOffset);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                using (GuardWriteScope())
                {
                    _vTable->CustomerCode_Setter(this, value);
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
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->FirstName_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 LastName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->LastName_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->LastName_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->LastName_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Pseudonyme
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Pseudonyme_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Pseudonyme_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Pseudonyme_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Presentation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Presentation_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Presentation_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Presentation_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 City
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->City_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->City_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->City_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Country
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Country_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Country_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Country_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 CompanyName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->CompanyName_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->CompanyName_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->CompanyName_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Address1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Address1_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Address1_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Address1_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Address2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Address2_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Address2_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Address2_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Address3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Address3_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Address3_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Address3_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 ZipCode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->ZipCode_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->ZipCode_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->ZipCode_MapEntryIndex);
                    }
                }
            }
        }

        public GhostStringUtf16 Hobbies
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    GuardLocalScope();
                    var stringArrayEntry = _data.Get<ArrayMapSmallEntry>(_vTable->Hobbies_MapEntryOffset);
                    return new GhostStringUtf16(this, _vTable->Hobbies_MapEntryIndex, _data.Slice((int)stringArrayEntry.ArrayOffset, stringArrayEntry.PhysicalSize));
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    using (GuardWriteScope())
                    {
                        _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->Hobbies_MapEntryIndex);
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
                        var union = Unsafe.As<BodyUnion>(this);
                        _vTable->Std.SwapAnyArray(union, MemoryMarshal.AsBytes(value.AsSpan()), _vTable->FirstName_MapEntryIndex);
                    }
                }
            }
        }

        /*
        private bool _mapped = false, _readOnly = false;
        private void ToStandalone() { }

        public string FirstNameStringInline
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
                
                using (GuardWriteScope())
                {
                    unsafe
                    {
                        if (_mapped)
                        {
                            if (_readOnly)
                                throw new InvalidOperationException("Cannot modify a read-only Ghost object.");
                            else
                                ToStandalone();
                        }
                        if (_vTable->FirstName_MapEntryOffset > 0)
                        {
                            _vTable->Std.SwapAnyArray(Unsafe.As<BodyUnion>(this), MemoryMarshal.AsBytes(value.AsSpan()), _vTable->FirstName_MapEntryIndex);
                        }
                    }
                }
            }
        }
        */
    }
}
