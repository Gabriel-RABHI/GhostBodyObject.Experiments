using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Structs
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct SegmentReference
    {
        // -----------------------------------------------------------------
        // PHYSICAL OVERLAYS
        // -----------------------------------------------------------------
        [FieldOffset(0)]
        public uint SegmentId;

        [FieldOffset(4)]
        public uint Offset;
    }
    }
