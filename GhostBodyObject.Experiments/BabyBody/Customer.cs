using GhostBodyObject.Common.Memory;
using GhostBodyObject.Common.Objects;
using GhostBodyObject.Common.SpinLocks;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Experiments.BabyBody
{

    public unsafe static class Customer_VectorRegistry
    {
        private static int _ghostSize;
        private static PinnedMemory<byte> _initialGhost;

        public static int GhostSize => _ghostSize;

        public static Customer_VectorTable* Standalone { get; private set; }

        static Customer_VectorRegistry()
        {
            Standalone = (Customer_VectorTable*)NativeMemory.Alloc((nuint)sizeof(Customer_VectorTable));
            var f = new GhostHeaderIncrementor();
            Standalone->CreatedOn_FieldOffset = f.Push<DateTime>();    // CreatedOn : 40
            Standalone->CustomerCode_FieldOffset = f.Push<int>();         // CustomerCode : 48
            Standalone->Active_FieldOffset = f.Push<bool>();        // Active : 49
            f.Padd(4); // -> 52
            Standalone->CustomerName_MapEntryOffset = f.Push<ArrayMapSmallEntry>();
            Standalone->CustomerCodeTiers_MapEntryOffset = f.Push<ArrayMapSmallEntry>();

            // -------- Function Pointers -------- //
            Standalone->Active_Setter = &Customer_VectorTables_Standalone.Active_Setter;
            Standalone->CustomerCode_Setter = &Customer_VectorTables_Standalone.CustomerCode_Setter;
            Standalone->CreatedOn_Setter = &Customer_VectorTables_Standalone.CreatedOn_Setter;

            _ghostSize = f.Offset;

            var buff = GC.AllocateArray<byte>(_ghostSize, true);
            _initialGhost = new PinnedMemory<byte>(buff,0,buff.Length);

            GhostHeader* header = (GhostHeader*)Unsafe.AsPointer(ref buff[0]);
            header->Id = new GhostId(GhostIdKind.Entity, 100, 0, 0);
            header->ModelVersion = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe PinnedMemory<byte> CreateGhost()
        {
            var ghost = TransientGhostMemoryAllocator.Allocate(GhostSize);
            ref byte srcRef = ref MemoryMarshal.GetReference(_initialGhost.Span);
            ref byte destRef = ref MemoryMarshal.GetReference(ghost.Span);
            Unsafe.CopyBlock(ref destRef, ref srcRef, (uint)GhostSize);
            return ghost;
        }
    }

    public static class Customer_VectorTables_Standalone
    {
        public static unsafe void SwapAnyArray(Customer body, Memory<byte> src, int arrayIndex)
        {
        }

        public static unsafe void Active_Setter(Customer body, bool value)
        {
            Unsafe.As<BodyUnion>(body)._data.Set<bool>(body._vTable->Active_FieldOffset, value);
        }

        public static unsafe void CustomerCode_Setter(Customer body, int value)
        {
        }

        public static unsafe void CreatedOn_Setter(Customer body, DateTime value)
        {
        }
    }

    public unsafe struct Customer_VectorTable
    {
        // ---------------------------------------------------------
        // Standard Fields
        // ---------------------------------------------------------
        public VectorTable Std;

        // ---------------------------------------------------------
        // Value Field Offsets
        // ---------------------------------------------------------
        public int CreatedOn_FieldOffset;

        public int CustomerCode_FieldOffset;

        public int Active_FieldOffset;

        // ---------------------------------------------------------
        // Array Map Offsets
        // ---------------------------------------------------------
        public int CustomerName_MapEntryOffset;

        public int CustomerCodeTiers_MapEntryOffset;

        // ---------------------------------------------------------
        // Setters
        // ---------------------------------------------------------
        public delegate*<Customer, bool, void> Active_Setter;

        public delegate*<Customer, int, void> CustomerCode_Setter;

        public delegate*<Customer, DateTime, void> CreatedOn_Setter;
    }

    [EntityBody(100, 1, 4)]
    public interface MCustomer
    {
        [EntityProperty(1)]
        bool Active { get; set; }

        [EntityProperty(2)]
        DateTime CreatedOn { get; set; }

        [EntityProperty(3)]
        int CustomerCode { get; set; }

        [EntityProperty(4)]
        GhostString CustomerName { get; }

        [EntityProperty(5)]
        GhostString CustomerCodeTiers { get; }

        /*
        [FieldOffset(52)]
        public int CustomerName_Offset;
        [FieldOffset(56)]
        public int CustomerCodeTiers_Offset;
        [FieldOffset(60)]
        public int WareHouse_Offset;
        [FieldOffset(64)]
        public int TradeName_Offset;
        [FieldOffset(68)]
        public int PrimaryAddressStreet1_Offset;
        [FieldOffset(72)]
        public int PrimaryAddressStreet2_Offset;
        [FieldOffset(76)]
        public int PrimaryAddressStreet3_Offset;
        [FieldOffset(80)]
        public int PrimaryAddressZipCode_Offset;
        [FieldOffset(84)]
        public int PrimaryAddressCity_Offset;
        [FieldOffset(88)]
        public int CustomerCountryCode_Offset;
        [FieldOffset(92)]
        public int CustomerPhoneNumber_Offset;
        [FieldOffset(96)]
        public int CustomerEmail_Offset;
        [FieldOffset(100)]
        public int CustomerRepresentative_Offset;
        [FieldOffset(104)]
        public int TaxType_Offset;
        [FieldOffset(108)]
        public int PaymentMethodCode_Offset;
        */
    }

    public class GhostContext
    {

    }


    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 32)]
    public class Customer : IEntityBody
    {
        [FieldOffset(0)]
        private GhostContext _context;

        [FieldOffset(8)]
        private IntPtr _vTablePtr;

        [FieldOffset(16)]
        private PinnedMemory<byte> _data;

        internal unsafe Customer_VectorTable* _vTable
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Customer_VectorTable*)_vTablePtr;

            set => _vTablePtr = (IntPtr)value;
        }

        public Customer()
        {
            unsafe
            {
                _vTable = Customer_VectorRegistry.Standalone;
                _data = Customer_VectorRegistry.CreateGhost();
            }
        }

        public unsafe bool Active
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _data.Get<bool>(_vTable->Active_FieldOffset);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _vTable->Active_Setter(this, value);
        }

        public GhostString CustomerName
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    var stringOffset = _data.Get<ArrayMapSmallEntry>(_vTable->CustomerName_MapEntryOffset);
                    return new GhostString(this, _data.Slice((int)stringOffset.ArrayOffset, (int)stringOffset.ArrayLength));
                }
            }
        }
    }

    public class PocoCustomer
    {
        public bool Active { get; set; }
        public DateTime CreatedOn { get; set; }
        public int CustomerCode { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerCodeTiers { get; set; } = string.Empty;
    }
}
