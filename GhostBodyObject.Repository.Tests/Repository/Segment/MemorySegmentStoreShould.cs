using GhostBodyObject.Common.Memory;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Segment;
using GhostBodyObject.Repository.Repository.Structs;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace GhostBodyObject.Repository.Tests.Repository.Segment
{
    public class MemorySegmentStoreShould
    {
        [Fact]
        public void AutoResizeWhenAddingMoreSegmentsThanInitialCapacity()
        {
            var store = new MemorySegmentStore(SegmentStoreMode.InMemoryVolatileRepository);

            // Add enough segments to trigger resize
            for (int i = 0; i < 1024; i++)
            {
                store.CreateSegment(1024);
            }
        }

        [Fact]
        public unsafe void CommitTransaction_WritesCorrectStructure()
        {
            var store = new MemorySegmentStore(SegmentStoreMode.InMemoryVolatileRepository);
            
            // Create a fake body
            var ghostId = GhostId.NewId(GhostIdKind.Entity, 1);
            var body = new FakeBody(128, ghostId);
            
            // Mock stream
            var stream = new FakeStream(new List<BodyBase> { body });
            
            bool stored = false;
            
            // Use new API
            var ctx = store.ReserveTransaction(stream, 100);
            store.WriteTransaction(ctx, (id, r) =>
            {
                stored = true;
                Assert.Equal(ghostId, id);
            });
            
            Assert.True(stored);
            
            // Verify Memory
            var holders = store.GetHolders();
            var segment = holders.Holders[0].Segment;
            byte* ptr = segment.BasePointer;
            
            // 1. SegmentHeader (16 bytes)
            SegmentHeader* segHeader = (SegmentHeader*)ptr;
            Assert.Equal(SegmentStructureType.SegmentHeader, segHeader->T);
            
            // 2. TransactionHeader (32 bytes) at offset 16
            StoreTransactionHeader* txHeader = (StoreTransactionHeader*)(ptr + 16);
            Assert.Equal(SegmentStructureType.StoreTransactionHeader, txHeader->T);
            Assert.Equal((ulong)100, txHeader->Id);
            
            // 3. RecordHeader (8 bytes) at offset 16 + 32 = 48
            StoreTransactionRecordHeader* recHeader = (StoreTransactionRecordHeader*)(ptr + 48);
            Assert.Equal(SegmentStructureType.StoreTransactionRecordHeader, recHeader->T);
            Assert.Equal((uint)128, recHeader->Size);
            
            // 4. Ghost Data at offset 48 + 8 = 56. Length 128.
            GhostHeader* gHeader = (GhostHeader*)(ptr + 56);
            Assert.Equal(ghostId, gHeader->Id);
            Assert.Equal(100, gHeader->TxnId);
            Assert.Equal(GhostStatus.Mapped, gHeader->Status);
            
            // 5. TransactionEnd at offset 56 + 128 = 184.
            StoreTransactionEnd* txEnd = (StoreTransactionEnd*)(ptr + 184);
            Assert.Equal(SegmentStructureType.StoreTransactionEnd, txEnd->T);
            Assert.Equal((uint)1, txEnd->RecCount);
        }

        [Fact]
        public void ConcurrentCommitTransaction_WritesCorrectly()
        {
            var store = new MemorySegmentStore(SegmentStoreMode.InMemoryVolatileRepository);
            int threadCount = 4;
            int itemsPerThread = 100;
            
            var tasks = new Task[threadCount];
            var lockObj = new object();
            
            for(int i=0; i<threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() => {
                    for(int j=0; j<itemsPerThread; j++)
                    {
                        var body = new FakeBody(16, GhostId.NewId(GhostIdKind.Entity, 1));
                        var stream = new FakeStream(new List<BodyBase>{body});
                        
                        TransactionContext ctx = null;
                        lock(lockObj)
                        {
                            ctx = store.ReserveTransaction(stream, threadId * 1000 + j);
                        }
                        // Write outside lock
                        store.WriteTransaction(ctx, (id, r) => {});
                    }
                });
            }
            
            Task.WaitAll(tasks);
            // If we are here, no crash occurred.
        }

        private class FakeBody : BodyBase
        {
            private byte[] _buffer;
            public FakeBody(int size, GhostId id)
            {
                _buffer = new byte[size];
                _data = new PinnedMemory<byte>(_buffer, 0, size);
                unsafe {
                    fixed (byte* p = _buffer) {
                        ((GhostHeader*)p)->Id = id;
                        ((GhostHeader*)p)->Status = GhostStatus.MappedModified;
                    }
                }
            }
        }

        private class FakeStream : IModifiedBodyStream
        {
            private List<BodyBase> _bodies;
            public FakeStream(List<BodyBase> bodies) { _bodies = bodies; }
            public void ReadModifiedBodies(Action<BodyBase> reader)
            {
                foreach (var b in _bodies) reader(b);
            }
        }
    }
}
