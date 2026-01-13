using GhostBodyObject.Repository.Repository.Constants;

namespace GhostBodyObject.Repository.Repository.Helpers
{
    public static class SegmentSizeComputation
    {
        public static int MB => 1024 * 1024;

        public static int GetNextSegmentSize(SegmentStoreMode _storeMode, int segmentCount, int recordSize = 0)
        {
            var r = 0;
            switch (_storeMode)
            {
                case SegmentStoreMode.InMemoryRepository:
                    if (segmentCount < 16)
                        r = 8 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 64)
                        r = 32 * MB; // 32 * 24 = 768 MB
                    r = 128 * MB;
                    break;
                case SegmentStoreMode.InMemoryLog:
                    r = 32 * MB;
                    break;
                case SegmentStoreMode.PersistantRepository:
                case SegmentStoreMode.PersistantLog:
                    if (segmentCount < 8)
                        r = 16 * MB; // 16 * 8 = 128 MB
                    if (segmentCount < 16)
                        r = 64 * MB; // 16 * 8 = 512 MB
                    if (segmentCount < 32)
                        r = 256 * MB; // 256 * 16 = 8 GB (8.6 GB total)
                    r = 1024 * MB;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported Segment Store Mode.");
            }

            if (recordSize > 0 && r < recordSize * 2)
            {
                if (recordSize >= 512 * MB)
                    throw new InvalidOperationException($"Unsupported Segment size (more the 1 GB) imposed by next record size ({recordSize}).");
                while (r < recordSize * 2)
                {
                    r *= 2;
                }
            }
            return r;
        }
    }
}
