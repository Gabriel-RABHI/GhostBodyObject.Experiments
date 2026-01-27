using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Concepts.RefStructBody.Body
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public unsafe struct EntitySlot
    {
        // 1. Ghost Data Pointer (8 bytes)
        [FieldOffset(0)]
        public byte* GhostPtr;

        // 2. Virtual Table Pointer (8 bytes)
        [FieldOffset(8)]
        public IntPtr VTablePtr;

        // 3. Sequence Guard (4 bytes)
        // Even = Clean, Odd = Dirty (Write in progress)
        [FieldOffset(16)]
        public int Sequence;
    }

    // ******** GENERIC ******** //
    public class TransactionContext
    {
    }

    /// <summary>
    /// 
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 24)]
    public unsafe partial struct BodyStruct
    {
        // The TransactionContext
        [FieldOffset(0)]
        private TransactionContext _context;

        [FieldOffset(8)]
        internal IntPtr _vTablePtr;

        // Contains a reference to a Segment or an Arena memory allocator
        [FieldOffset(16)]
        internal PinnedMemory<byte> _data;
    }

    /// <summary>
    /// Allocator of BodyStruct
    /// </summary>
    public unsafe class BodyStructArena
    {
        [ThreadStatic]
        private static BodyStruct[]? _buffer;

        [ThreadStatic]
        private static int _head;

        public static BodyHandleBase New()
        {

            BodyStruct[]? buffer = _buffer;
            int offset = _head;

            if (buffer == null || _head + 1 > (uint)buffer.Length)
            {
                buffer = GC.AllocateUninitializedArray<BodyStruct>(1024, pinned: true);
                _buffer = buffer;
                offset = 0;
            }
            _head++;
            throw new NotImplementedException();
            //return &_buffer[_head];
        }
    }

    public unsafe struct BodyHandleBase
    {
        public BodyStructArena _arena;
        public int _index;
    }

    // ******** SPECIFIC ******** //
    // Generated Code

    public static class CrmContext
    {
        public static TransactionContext Context;
    }

    public unsafe partial struct CrmUser
    {
        // 16 bytes
        private BodyHandleBase _bodyHandle;

        public CrmUser()
        {
            // Allocate a new BodyStruct in the BodyStructArena of the current TransactionContext
        }

        public TransactionContext Context {
            get; set;
        }

        public DateTime BirthDate {
            get {
                //if (_bodyHandle._arena.Context != CrmContext.Context)
                    throw new Exception();
                //_bodyHandle._body->_data.Get<DateTime>(/*_data->_vTable->CustomerCode_FieldOffset*/15);
                return default;
            }
            set {
                throw new NotImplementedException(); 
            }
        }
    }
}
