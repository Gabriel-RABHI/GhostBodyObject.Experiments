namespace GhostBodyObject.Repository.Repository.Constants
{
    public enum SegmentTransactionOrigin : byte
    {
        Repository = 1,
        Compactor = 2,
        Replication = 4
    }
}
