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

#undef NULL_RETURN_PRINCIPLE

using System.IO;
using System.Runtime.CompilerServices;
using GhostBodyObject.Common.Memory;
using GhostBodyObject.Common.SpinLocks;
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

        private volatile MemorySegmentHolder[] _segmentHolders;
        private volatile MemorySegmentHolder _currentHolder;
        private byte*[] _segmentPointers;
        private int _newSegmentIndex = 0;
        private int _segmentCount = 0;
        private int _transactionCount = 0;
        private ShortSpinLock _commitLocker;

        // MMF Configuration
        private string _directoryPath;
        private string _prefix = "store";
        private string _name = "data";

        public SegmentImplementationType ImplementationType => _implementationType;

        public SegmentStoreMode StoreMode { get; private set; }

        public bool IsPersistant => _isPersistent;

        public bool IsCommiting
        {
            get
            {
                if (_commitLocker.TryEnter())
                {
                    _commitLocker.Exit();
                    return false;
                }
                return true;
            }
        }

        public MemorySegmentStore(SegmentStoreMode mode, string directoryPath = null)
        {
            _storeMode = mode;
            _implementationType = mode.ImplementationMode();
            _isPersistent = mode.IsPersistent();
            _isCompactable = mode.IsCompactable();
            _directoryPath = directoryPath ?? Directory.GetCurrentDirectory();

            _segmentHolders = new MemorySegmentHolder[1];
            _segmentPointers = new byte*[1];
        }

        ~MemorySegmentStore()
        {
            Dispose();
        }

        public void RebuildSegmentHolders()
        {
            if (_transactionCount == 0 || !_segmentHolders.Any(h => h != null && h.ReferenceCount == 0))
                return;
            if (_commitLocker.TryEnter())
            {
                try
                {
                    _segmentCount = 0;
                    var l = _segmentHolders.Length;
                    var newHolders = new MemorySegmentHolder[l];
                    var newPointers = new byte*[l];
                    for (int i = 0; i < l; i++)
                    {
                        if (_segmentHolders[i] != null)
                        {
                            if (_segmentHolders[i].ReferenceCount > 0 || _segmentHolders[i] == _currentHolder)
                            {
                                newHolders[i] = _segmentHolders[i];
                                newPointers[i] = _segmentPointers[i];
                                _segmentCount++;
                            }
                        }
                    }
                    if (_transactionCount > 0 && !newHolders.Any(s => s != null))
                        throw new InvalidOperationException();
                    if (_transactionCount > 0 && newHolders[newHolders.Length - 1] == null)
                        throw new InvalidOperationException();
                    _segmentHolders = newHolders;
                    _segmentPointers = newPointers;
                }
                finally
                {
                    _commitLocker.Exit();
                }
            }
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
            if (!IsCommiting)
                RebuildSegmentHolders();
        }

        public int CreateSegment(int capacity)
        {
            MemorySegment segment = null;
            switch (_implementationType)
            {
                case SegmentImplementationType.LOHPinnedMemory:
                    segment = MemorySegment.NewInMemory(_storeMode, _newSegmentIndex, capacity);
                    _newSegmentIndex++;
                    break;
                case SegmentImplementationType.ProtectedMemoryMappedFile:
                    segment = MemorySegment.NewMemoryMapped(_storeMode, _newSegmentIndex, capacity, _isCompactable, _directoryPath, _prefix, _name);
                    _newSegmentIndex++;
                    break;
            }
            if (segment == null)
                throw new InvalidOperationException("Segment creation failed.");
            return AddSegment(segment);
        }

        private int AddSegment(MemorySegment segment)
        {
            var index = segment.Index;
            var minLenght = index + 1;
            if (minLenght > _segmentHolders.Length)
            {
                if (SegmentSizeComputation.SmallSegmentsMode)
                {
                    Array.Resize(ref _segmentHolders, minLenght);
                    var newPointers = new byte*[_segmentPointers.Length + 1];
                    Array.Copy(_segmentPointers, newPointers, _segmentPointers.Length);
                    _segmentPointers = newPointers;
                }
                else
                {
                    Array.Resize(ref _segmentHolders, _segmentHolders.Length * 2);
                    var newPointers = new byte*[_segmentPointers.Length * 2];
                    Array.Copy(_segmentPointers, newPointers, _segmentPointers.Length);
                    _segmentPointers = newPointers;
                }
            }
            _currentHolder = new MemorySegmentHolder(segment);
            _segmentHolders[index] = _currentHolder;
            _segmentPointers[index] = segment.BasePointer;
            _segmentCount++;
            return index;
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

        public bool WriteTransaction<T>(T commiter, GhostRepositoryTransactionIdRange range, Action<GhostId, SegmentReference> onGhostStored)
             where T : IModifiedBodyStream
        {
            var bodies = new List<BodyBase>();
            commiter.ReadModifiedBodies(b => bodies.Add(b));

            if (bodies.Count == 0)
                return false;

            TransactionChecksum checksum = null;
            if (_isPersistent)
                checksum = new TransactionChecksum();

            _commitLocker.Enter();
            try
            {
                var txnId = range.TopTransactionId + 1;
                if (_currentHolder == null)
                {
                    CreateSegment(SegmentSizeComputation.GetNextSegmentSize(_storeMode, _segmentCount));
                }
                // -------- Initialize Transaction Context Variables
                int startSegmentId = _currentHolder.Index;
                int startOffset = _currentHolder.Segment.Reserve(0);
                bool isSplit = false;
                int currentSegmentId = startSegmentId;
                int currentOffset = startOffset;

                // -------- Reserve & Write Transaction Header
                int headerSize = GetSize.Of8Aligned<StoreTransactionHeader>();
                if (!CheckFit(currentSegmentId, headerSize))
                {
                    HandleSegmentJump(ref currentSegmentId, ref currentOffset, ref isSplit);
                    startSegmentId = currentSegmentId;
                    startOffset = currentOffset;
                }

                var txHeader = new StoreTransactionHeader
                {
                    T = SegmentStructureType.StoreTransactionHeader,
                    Origin = SegmentTransactionOrigin.Repository,
                    Id = (ulong)txnId,
                    PreviousOffset = 0,
                    PreviousSegmentId = 0
                };

                var segment = _segmentHolders[currentSegmentId].Segment;
                segment.Reserve(headerSize);
                // startOffset = segment.Reserve(0); // Removed: Wrongly overwrites startOffset
                segment.WriteAt(currentOffset, txHeader);
                if (_isPersistent) checksum.Write(txHeader);
                currentOffset += headerSize;

                // -------- Process Bodies
                for (int i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];
                    int ghostSize = GetSize.Of(body._data);
                    int recordSize = GetSize.Of8Aligned<StoreTransactionRecordHeader>(ghostSize);

                    if (!CheckFit(currentSegmentId, recordSize))
                    {
                        HandleSegmentJump(ref currentSegmentId, ref currentOffset, ref isSplit);
                        segment = _segmentHolders[currentSegmentId].Segment; // Update segment reference after jump
                    }

                    // Reserve Space
                    segment.Reserve(recordSize);
                    int writeOffset = currentOffset;
                    currentOffset += recordSize;

                    // Write Record Header
                    var recordHeader = new StoreTransactionRecordHeader
                    {
                        T = SegmentStructureType.StoreTransactionRecordHeader,
                        Origin = SegmentTransactionOrigin.Repository,
                        Size = (uint)ghostSize
                    };
                    segment.WriteAt(writeOffset, recordHeader);
                    if (_isPersistent) checksum.Write(recordHeader);

                    // Write Ghost Data
                    var ghostDataPtr = segment.WriteBytesAt(writeOffset + sizeof(StoreTransactionRecordHeader), body._data.Ptr, ghostSize);

                    // Update Ghost Header in place
                    var h = (GhostHeader*)ghostDataPtr;
                    h->TxnId = txnId;
                    h->Status = body.Status == GhostStatus.MappedDeleted ? GhostStatus.Tombstone : GhostStatus.Mapped;

                    if (_isPersistent) checksum.Write(body._data.Ptr, ghostSize);

                    // Callback
                    onGhostStored(body.Id, new SegmentReference { SegmentId = (uint)currentSegmentId, Offset = (uint)(writeOffset + sizeof(StoreTransactionRecordHeader)) });
                }

                // -------- Reserve & Write Transaction End
                int endSize = sizeof(StoreTransactionEnd);
                if (!CheckFit(currentSegmentId, endSize))
                {
                    HandleSegmentJump(ref currentSegmentId, ref currentOffset, ref isSplit);
                    segment = _segmentHolders[currentSegmentId].Segment; // Update segment reference after jump
                }

                segment.Reserve(endSize);
                int endOffset = currentOffset;
                currentOffset += endSize;

                var txEnd = new StoreTransactionEnd
                {
                    T = SegmentStructureType.StoreTransactionEnd,
                    RecCount = (uint)bodies.Count
                };
                segment.WriteAt(endOffset, txEnd);
                if (_isPersistent) checksum.Write(txEnd);

                // -------- Finalize Persistence
                if (_isPersistent)
                {
                    var startSeg = _segmentHolders[startSegmentId].Segment;
                    ulong hash = checksum.GetHash();
                    startSeg.WriteAt(startOffset + 24, hash);

                    if (isSplit)
                    {
                        for (int id = startSegmentId; id <= currentSegmentId; id++)
                        {
                            var s = _segmentHolders[id].Segment;
                            int start = (id == startSegmentId) ? startOffset : 0;

                            if (id == currentSegmentId)
                            {
                                int end = currentOffset;
                                s.FlushRange(start, end - start);
                            }
                            else
                            {
                                int end = s.Capacity - s.FreeSpace;
                                s.FlushRange(start, end - start);
                                s.FlushRange(s.Capacity - 8, 8); // Flush Footer
                            }
                        }
                    }
                    else
                    {
                        startSeg.FlushRange(startOffset, currentOffset - startOffset);
                    }
                }
                return true;
            }
            finally
            {
                if (checksum != null)
                    checksum.Dispose();
                _transactionCount++;
                range.IncrementTopTransactionId();
                _commitLocker.Exit();
            }
        }

        private bool CheckFit(int segmentId, int size)
        {
            var closingSize = sizeof(StoreTransactionSegmentJump) + sizeof(SealedSegmentEnd) + sizeof(SealedSegmentFooter);
            return _segmentHolders[segmentId].Segment.FreeSpace >= (size + closingSize);
        }

        private void HandleSegmentJump(ref int currentSegmentId, ref int currentOffset, ref bool isSplit)
        {
            var currentSeg = _segmentHolders[currentSegmentId].Segment;

            var jump = new StoreTransactionSegmentJump
            {
                T = SegmentStructureType.StoreTransactionSegmentJump,
            };

            int jumpOffset = currentSeg.Reserve(sizeof(StoreTransactionSegmentJump));
            currentSeg.WriteAt(jumpOffset, jump);

            var sealEnd = new SealedSegmentEnd
            {
                T = SegmentStructureType.SealedSegmentEnd,
                NextSegmentId = (uint)(currentSegmentId + 1)
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
            currentSegmentId++;
            isSplit = true;

            var nextSeg = _segmentHolders[currentSegmentId].Segment;
            var continuation = new StoreTransactionContinuation
            {
                T = SegmentStructureType.StoreTransactionContinuation,
                PreviousSegmentId = (uint)(currentSegmentId - 1)
            };
            int contOffset = nextSeg.Reserve(sizeof(StoreTransactionContinuation));
            nextSeg.WriteAt(contOffset, continuation);

            currentOffset = contOffset + sizeof(StoreTransactionContinuation);
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
