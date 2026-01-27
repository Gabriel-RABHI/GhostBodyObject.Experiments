using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Concepts.RefStructBody.Body
{
    public partial struct CrmUser
    {
        public TimeSpan Age => DateTime.Now - BirthDate;

    }


    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public unsafe struct LargeEntitySlot
    {
        // [0-7] Data Pointer
        [FieldOffset(0)] public byte* GhostPtr;

        // [8-15] VTable Pointer
        [FieldOffset(8)] public IntPtr VTablePtr;

        // [16-19] Concurrency Guard
        [FieldOffset(16)] public int Sequence;

        // [20-23] Owner Index (Points to the Managed Array)
        // This links the "Unmanaged Slot" to the "Managed Owner"
        [FieldOffset(20)] public int OwnerID;
    }

    public unsafe class BloggerTransaction : IDisposable
    {
        // ... Thread Affinity logic ...

        // PAGE CONFIGURATION
        private const int PAGE_SIZE = 4096;
        private const int PAGE_MASK = 4095;
        private const int PAGE_SHIFT = 12;

        // 1. Unmanaged Pages (POH) - The Slots
        private readonly List<LargeEntitySlot[]> _slotPages = new();

        // 2. Managed Pages (Heap) - The Owners
        // Replaces the HashSet. Mirrors _slotPages structure.
        private readonly List<object[]> _ownerPages = new();

        private int _nextGlobalId = 0;

        
        ~BloggerTransaction()
        {
            Dispose();
        }
        

        public LargeEntitySlot* AllocateSlot(byte* initialGhost, IntPtr initialVTable, object owner)
        {
            // 1. Calculate Page and Offset
            // We simply increment a global counter.
            int globalId = _nextGlobalId++;
            int pageIndex = globalId >> PAGE_SHIFT;
            int slotIndex = globalId & PAGE_MASK;

            // 2. Expand Pages if needed
            if (slotIndex == 0) AddPage();

            // 3. Store the Managed Owner (Parallel Array)
            // This keeps the Segment/Arena alive.
            _ownerPages[pageIndex][slotIndex] = owner;

            // 4. Initialize the Unmanaged Slot (POH)
            // We don't need fixed{} because the array is pinned.
            LargeEntitySlot* slot = (LargeEntitySlot*)Unsafe.AsPointer(ref _slotPages[pageIndex][slotIndex]);

            slot->GhostPtr = initialGhost;
            slot->VTablePtr = initialVTable;
            slot->Sequence = 0;
            slot->OwnerID = globalId; // Store the ID to allow lookups later

            return slot;
        }

        private void AddPage()
        {
            // Alloc POH Page (Pinned)
            _slotPages.Add(GC.AllocateUninitializedArray<LargeEntitySlot>(PAGE_SIZE, pinned: true));
            // Alloc Parallel Managed Page
            _ownerPages.Add(new object[PAGE_SIZE]);
        }

        // --- CRITICAL FIX: The CoW Logic ---
        /*
        public void EnsureWritable(LargeEntitySlot* slot, int requiredSize)
        {
            // 1. Allocation Logic (Create new Arena Memory)
            // byte* newGhost = _arenaAllocator.Alloc(requiredSize);
            // object newOwner = _arenaAllocator.CurrentPage;

            // 2. Update the Slot Pointers
            // slot->GhostPtr = newGhost;

            // 3. Update the Managed Owner (Release the old one!)
            int id = slot->OwnerID;
            int page = id >> PAGE_SHIFT;
            int index = id & PAGE_MASK;

            // OVERWRITE: The old 'Segment' or 'Arena' reference is dropped here.
            // If 100M updates happen, the GC collects the 99,999,999 old ones.
            _ownerPages[page][index] = newOwner;
        }
        */

        /// <summary>
        /// Performs a structural update of the entity slot.
        /// This updates both the Unmanaged Pointers (for the App) and the Managed Owner (for the GC).
        /// </summary>
        /// <remarks>
        /// SAFETY: The caller (EntityProxy) MUST hold the Write Guard and have incremented 
        /// the Slot Sequence (Seqlock) before calling this method.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateEntity(LargeEntitySlot* slot, byte* newGhost, IntPtr newVTable, object newOwner)
        {
            // 1. Thread Affinity Check
            // (Optional if strictly called by Proxy which already checks, but good for safety)
            //if (OwnerThreadId != Environment.CurrentManagedThreadId)
            //    throw new InvalidOperationException("Cross-thread access detected during update.");

            // 2. Resolve the Managed Index
            // We retrieve the location of the "Owner" reference using the ID stored in the Slot.
            int globalId = slot->OwnerID;
            int pageIndex = globalId >> PAGE_SHIFT;
            int slotIndex = globalId & PAGE_MASK;

            // 3. Update Managed Owner (Parallel Array)
            // CRITICAL: This overwrites the old owner. 
            // If the old owner was a transient Arena, it is now unreachable and eligible for GC.
            // This prevents the "Memory Leak by History".
            _ownerPages[pageIndex][slotIndex] = newOwner;

            // 4. Update Unmanaged Pointers (POH)
            // We update the pointers to the new memory location.
            // Readers spinning on the Seqlock will see these new values when they retry.
            slot->GhostPtr = newGhost;
            slot->VTablePtr = newVTable;
        }

        public void Dispose()
        {
            // Dropping these lists allows GC to collect all Owners and POH arrays
            _slotPages.Clear();
            _ownerPages.Clear();
        }
    }
}
