using GhostBodyObject.Repository.Repository.Constants;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public static class SegmentStoreModeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SegmentImplementationType ImplementationMode(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.InMemoryLog:
                    return SegmentImplementationType.LOHPinnedMemory;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    return SegmentImplementationType.ProtectedMemoryMappedFile;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPersistent(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.InMemoryLog:
                    return false;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    return true;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCompactable(this SegmentStoreMode mode)
        {
            switch (mode)
            {
                case SegmentStoreMode.InMemoryRepository:
                case SegmentStoreMode.PersistantRepository:
                    return true;
                case SegmentStoreMode.InMemoryLog:
                case SegmentStoreMode.PersistantLog:
                    return false;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }
        }
    }
}
