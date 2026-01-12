namespace GhostBodyObject.Repository.Repository.Constants
{
    public enum SegmentStructureType : byte
    {
        SegmentHeader = 1,
        StoreTransactionHeader = 2,
        StoreTransactionRecordHeader = 3,
        StoreTransactionSegmentJump = 4,
        StoreTransactionContinuation = 5,
        StoreTransactionEnd = 6,
        SealedSegmentEnd = 7,
        SealedSegmentFooter = 8
    }
}
