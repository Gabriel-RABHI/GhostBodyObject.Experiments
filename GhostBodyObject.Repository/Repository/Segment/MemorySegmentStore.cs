/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

#define NULL_RETURN_PRINCIPLE

using System.IO;
using System.Runtime.CompilerServices;
using GhostBodyObject.Common.Memory;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Constants;
using GhostBodyObject.Repository.Repository.Contracts;
using GhostBodyObject.Repository.Repository.Helpers;
using GhostBodyObject.Repository.Repository.Structs;

namespace GhostBodyObject.Repository.Repository.Segment
{

    public sealed unsafe class MemorySegmentStore : ISegmentStore, IDisposable
    {
        private SegmentStoreMode _storeMode;
        private SegmentImplementationType _implementationType;
        private bool _isPersistent = false;
        private bool _isCompactable = false;

        private MemorySegmentHolder[] _segmentHolders;
        private MemorySegmentHolder _currentHolder;
        private byte*[] _segmentPointers;
        private int _lastSegmentId = 0;
        private int _segmentCount = 0;

        // MMF Configuration
        private string _directoryPath;
        private string _prefix = "store";
        private string _name = "data";

        public SegmentImplementationType ImplementationType => _implementationType;

        public SegmentStoreMode StoreMode { get; private set; }

        public bool IsPersistant => _isPersistent;

        public MemorySegmentStore(SegmentStoreMode mode, string directoryPath = null)
        {
            _storeMode = mode;
            _implementationType = mode.ImplementationMode();
            _isPersistent = mode.IsPersistent();
            _isCompactable = mode.IsCompactable();
            _directoryPath = directoryPath ?? Directory.GetCurrentDirectory();

            _segmentHolders = new MemorySegmentHolder[8];
            _segmentPointers = new byte*[8];
        }

        ~MemorySegmentStore()
        {
            Dispose();
        }

        public void RebuildSegmentHolders()
        {
            if (!_segmentHolders.Any(h => h != null && h.ReferenceCount == 0))
                return;
            var newHolders = new MemorySegmentHolder[_segmentHolders.Length];
            var newPointers = new byte*[_segmentPointers.Length];
            for (int i = 0; i < _segmentHolders.Length; i++)
            {
                if (_segmentHolders[i] != null)
                {
                    if (_segmentHolders[i].ReferenceCount > 0)
                    {
                        newHolders[i] = _segmentHolders[i];
                        newPointers[i] = _segmentPointers[i];
                    }
                } 
            }
            _segmentHolders = newHolders;
            _segmentPointers = newPointers;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IncrementSegmentHolderUsage(SegmentReference reference)
        {
            var ghostH = ToGhostHeaderPointer(reference);
            var recordH = (StoreTransactionRecordHeader*)((byte*)ghostH - sizeof(StoreTransactionRecordHeader));
            long size = recordH->Size + sizeof(StoreTransactionRecordHeader);
            _segmentHolders[reference.SegmentId].IncrementUsage(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DecrementSegmentHolderUsage(SegmentReference reference)
        {
            var ghostH = ToGhostHeaderPointer(reference);
            var recordH = (StoreTransactionRecordHeader*)((byte*)ghostH - sizeof(StoreTransactionRecordHeader));
            long size = recordH->Size + sizeof(StoreTransactionRecordHeader);
            if (_segmentHolders[reference.SegmentId].DecrementUsage(size))
            {
                // Cannot be done immediatly, because if the object simply moved, and the segment
                // have only one reference, it will be disposed while still in use.
                // -> RebuildSegmentHolders();
            }
        }

        /// <summary>
        /// Gets an array containing all current memory segment holders. Is used by Transactions to keep segments alive.
        /// </summary>
        /// <returns>An array of <see cref="MemorySegmentHolder"/> objects representing the current memory segment holders. The
        /// array may be empty if no holders are present.</returns>
        public MemorySegmentStoreHolders GetHolders()
        {
            return new MemorySegmentStoreHolders(_segmentHolders);
        }

        public void UpdateHolders(long bottomTxnId, long topTxnId)
        {
            // -------- Signal the GC it must clean up because a Txn that change bottom txn id with closed
        }

        public int CreateSegment(int capacity)
        {
            MemorySegment segment = null;
            switch (_implementationType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    segment = MemorySegment.NewInMemory(_storeMode, _lastSegmentId, capacity);
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    segment = MemorySegment.NewMemoryMapped(_storeMode, _lastSegmentId, capacity, _isCompactable, _directoryPath, _prefix, _name);
                    break;
            }
            if (segment == null)
                throw new InvalidOperationException("Segment creation failed.");
            return AddSegment(segment);
        }

        private int AddSegment(MemorySegment segment)
        {
            int segmentId = _lastSegmentId;

            if (segmentId >= _segmentHolders.Length)
            {
                if (SegmentSizeComputation.SmallSegmentsMode)
                {
                    Array.Resize(ref _segmentHolders, _segmentHolders.Length + 1);
                    var newPointers = new byte*[_segmentPointers.Length + 1];
                    Array.Copy(_segmentPointers, newPointers, _segmentPointers.Length);
                    _segmentPointers = newPointers;
                } else
                {
                    Array.Resize(ref _segmentHolders, _segmentHolders.Length * 2);
                    var newPointers = new byte*[_segmentPointers.Length * 2];
                    Array.Copy(_segmentPointers, newPointers, _segmentPointers.Length);
                    _segmentPointers = newPointers;
                }
                   
            }

            _currentHolder = new MemorySegmentHolder(segment, segmentId);
            _segmentHolders[segmentId] = _currentHolder;
            _segmentPointers[segmentId] = segment.BasePointer;
            _lastSegmentId++;
            _segmentCount++;
            return segmentId;
        }

#if DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
        {
#if NULL_RETURN_PRINCIPLE
            if (_segmentPointers[reference.SegmentId] == null)
                return null;
            if (_segmentHolders[reference.SegmentId] == null)
                return null;
#else
            if (_segmentPointers[reference.SegmentId] == null)
                throw new NullReferenceException($"The pointer {reference.SegmentId} do not exists.");
            if (_segmentHolders[reference.SegmentId] == null)
                throw new NullReferenceException($"The segment {reference.SegmentId} do not exists.");
#endif
            if (reference.Offset > _segmentHolders[reference.SegmentId].Segment.Capacity)
                throw new OverflowException($"The offset is superior to segment size.");

            return (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
#if NULL_RETURN_PRINCIPLE
            if (_segmentPointers[reference.SegmentId] == null)
                return PinnedMemory<byte>.Empty;
            if (_segmentHolders[reference.SegmentId] == null)
                return PinnedMemory<byte>.Empty;
#else
            if (_segmentPointers[reference.SegmentId] == null)
                throw new NullReferenceException($"The pointer {reference.SegmentId} do not exists.");
            if (_segmentHolders[reference.SegmentId] == null)
                throw new NullReferenceException($"The segment {reference.SegmentId} do not exists.");
#endif
            if (reference.Offset > _segmentHolders[reference.SegmentId].Segment.Capacity)
                throw new OverflowException($"The offset is superior to segment size.");

            var h = ToGhostHeaderPointer(reference);
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b - 1));
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GhostHeader* ToGhostHeaderPointer(SegmentReference reference)
            => (GhostHeader*)(_segmentPointers[reference.SegmentId] + reference.Offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PinnedMemory<byte> ToGhost(SegmentReference reference)
        {
            var h = ToGhostHeaderPointer(reference);
            var b = (int*)h;
            return new PinnedMemory<byte>(_segmentHolders[reference.SegmentId], h, *(b - 1));
        }
#endif

        public SegmentReference StoreGhost(PinnedMemory<byte> ghost, long txnId)
        {
            throw new NotSupportedException("Use CommitTransaction instead.");
        }

        public TransactionContext ReserveTransaction<T>(T commiter, long txnId)
             where T : IModifiedBodyStream
        {
            var bodies = new List<BodyBase>();
            commiter.ReadModifiedBodies(b => bodies.Add(b));

            if (bodies.Count == 0) return null;

            if (_currentHolder == null)
            {
                CreateSegment(SegmentSizeComputation.GetNextSegmentSize(_storeMode, _segmentCount));
            }

            var ctx = new TransactionContext
            {
                StartSegmentId = _currentHolder.Index,
                StartOffset = _currentHolder.Segment.Reserve(0),
                IsSplit = false,
                CurrentSegmentId = _currentHolder.Index,
                CurrentOffset = _currentHolder.Segment.Reserve(0),
                Bodies = bodies,
                TransactionId = txnId,
                BodyLocations = new (int SegmentId, int Offset)[bodies.Count]
            };

            // Reserve Transaction Header
            int headerSize = GetSize.Of8Aligned<StoreTransactionHeader>();
            if (!CheckFit(ctx.CurrentSegmentId, headerSize))
            {
                HandleSegmentJump(ctx);
            }
            ReserveSpace(ctx, headerSize);

            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                int ghostSize = GetSize.Of(body._data);
                int recordSize = GetSize.Of8Aligned<StoreTransactionRecordHeader>(ghostSize);

                if (!CheckFit(ctx.CurrentSegmentId, recordSize))
                {
                    HandleSegmentJump(ctx);
                }

                ctx.BodyLocations[i] = (ctx.CurrentSegmentId, ctx.CurrentOffset);
                ReserveSpace(ctx, recordSize);
            }

            // Reserve Transaction End
            int endSize = sizeof(StoreTransactionEnd);
            if (!CheckFit(ctx.CurrentSegmentId, endSize))
            {
                HandleSegmentJump(ctx);
            }
            ctx.EndSegmentId = ctx.CurrentSegmentId;
            ctx.EndOffset = ctx.CurrentOffset;
            ReserveSpace(ctx, endSize);

            return ctx;
        }

        public void WriteTransaction(TransactionContext ctx, Action<GhostId, SegmentReference> onGhostStored)
        {
            if (ctx == null)
                return;
            TransactionChecksum checksum = null;
            if (_isPersistent)
                checksum = new TransactionChecksum();
            try
            {
                var startSeg = _segmentHolders[ctx.StartSegmentId].Segment;
                var txHeader = new StoreTransactionHeader
                {
                    T = SegmentStructureType.StoreTransactionHeader,
                    Origin = SegmentTransactionOrigin.Repository,
                    Id = (ulong)ctx.TransactionId,
                    PreviousOffset = 0,
                    PreviousSegmentId = 0
                };
                startSeg.WriteAt(ctx.StartOffset, txHeader);
                if (_isPersistent)
                    checksum.Write(txHeader);
                for (int i = 0; i < ctx.Bodies.Count; i++)
                {
                    var body = ctx.Bodies[i];
                    var loc = ctx.BodyLocations[i];
                    var segment = _segmentHolders[loc.SegmentId].Segment;
                    int ghostSize = GetSize.Of(body._data);
                    var recordHeader = new StoreTransactionRecordHeader
                    {
                        T = SegmentStructureType.StoreTransactionRecordHeader,
                        Origin = SegmentTransactionOrigin.Repository,
                        Size = (uint)ghostSize
                    };
                    segment.WriteAt(loc.Offset, recordHeader);
                    if (_isPersistent)
                        checksum.Write(recordHeader);
                    var ghostDataPtr = segment.WriteBytesAt(loc.Offset + sizeof(StoreTransactionRecordHeader), body._data.Ptr, ghostSize);
                    var h = (GhostHeader*)ghostDataPtr;
                    h->TxnId = ctx.TransactionId;
                    h->Status = body.Status == GhostStatus.MappedDeleted ? GhostStatus.Tombstone : GhostStatus.Mapped;
                    if (_isPersistent)
                        checksum.Write(body._data.Ptr, ghostSize);
                    onGhostStored(body.Id, new SegmentReference { SegmentId = (uint)loc.SegmentId, Offset = (uint)(loc.Offset + sizeof(StoreTransactionRecordHeader)) });
                }
                var endSeg = _segmentHolders[ctx.EndSegmentId].Segment;
                var txEnd = new StoreTransactionEnd
                {
                    T = SegmentStructureType.StoreTransactionEnd,
                    RecCount = (uint)ctx.Bodies.Count
                };
                endSeg.WriteAt(ctx.EndOffset, txEnd);
                if (_isPersistent)
                    checksum.Write(txEnd);
                if (_isPersistent)
                {
                    ulong hash = checksum.GetHash();
                    startSeg.WriteAt(ctx.StartOffset + 24, hash);
                }
                if (_isPersistent)
                {
                    if (ctx.IsSplit)
                    {
                        for (int id = ctx.StartSegmentId; id <= ctx.CurrentSegmentId; id++)
                        {
                            var segment = _segmentHolders[id].Segment;
                            int start = (id == ctx.StartSegmentId) ? ctx.StartOffset : 0;

                            if (id == ctx.CurrentSegmentId)
                            {
                                int end = ctx.CurrentOffset;
                                segment.FlushRange(start, end - start);
                            }
                            else
                            {
                                int end = segment.Capacity - segment.FreeSpace;
                                segment.FlushRange(start, end - start);

                                // Flush Footer
                                segment.FlushRange(segment.Capacity - 8, 8);
                            }
                        }
                    }
                    else
                    {
                        startSeg.FlushRange(ctx.StartOffset, ctx.CurrentOffset - ctx.StartOffset);
                    }
                }
            }
            finally
            {
                if (checksum != null)
                    checksum.Dispose();
                RebuildSegmentHolders();
            }
        }

        private bool CheckFit(int segmentId, int size)
        {
            return _segmentHolders[segmentId].Segment.FreeSpace >= (size + 24);
        }

        private void ReserveSpace(TransactionContext ctx, int size)
        {
            _segmentHolders[ctx.CurrentSegmentId].Segment.Reserve(size);
            ctx.CurrentOffset += size;
        }

        private void HandleSegmentJump(TransactionContext ctx)
        {
            var currentSeg = _segmentHolders[ctx.CurrentSegmentId].Segment;

            var jump = new StoreTransactionSegmentJump
            {
                T = SegmentStructureType.StoreTransactionSegmentJump,
            };

            int jumpOffset = currentSeg.Reserve(sizeof(StoreTransactionSegmentJump));
            currentSeg.WriteAt(jumpOffset, jump);

            var sealEnd = new SealedSegmentEnd
            {
                T = SegmentStructureType.SealedSegmentEnd,
                NextSegmentId = (uint)(ctx.CurrentSegmentId + 1)
            };
            int sealOffset = currentSeg.Reserve(sizeof(SealedSegmentEnd));
            currentSeg.WriteAt(sealOffset, sealEnd);

            var footer = new SealedSegmentFooter
            {
                T = SegmentStructureType.SealedSegmentFooter,
                SegmentEndOffset = (uint)(sealOffset + sizeof(SealedSegmentEnd))
            };
            currentSeg.WriteAt(currentSeg.Capacity - sizeof(SealedSegmentFooter), footer);

            CreateSegment(SegmentSizeComputation.GetNextSegmentSize(_storeMode, _segmentCount));
            ctx.CurrentSegmentId++;
            ctx.IsSplit = true;

            var nextSeg = _segmentHolders[ctx.CurrentSegmentId].Segment;
            var continuation = new StoreTransactionContinuation
            {
                T = SegmentStructureType.StoreTransactionContinuation,
                PreviousSegmentId = (uint)(ctx.CurrentSegmentId - 1)
            };
            int contOffset = nextSeg.Reserve(sizeof(StoreTransactionContinuation));
            nextSeg.WriteAt(contOffset, continuation);

            ctx.CurrentOffset = contOffset + sizeof(StoreTransactionContinuation);
        }

        public void Dispose()
        {
            _currentHolder = null;
            for (int i = 0; i < _segmentHolders.Length; i++)
            {
                if (_segmentHolders[i] != null)
                {
                    _segmentHolders[i].Dispose();
                    _segmentHolders[i] = null;
                }
            }
            _segmentPointers = null;
            GC.SuppressFinalize(this);
        }
    }
}
