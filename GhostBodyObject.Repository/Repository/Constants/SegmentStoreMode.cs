namespace GhostBodyObject.Repository.Repository.Constants
{
    public enum SegmentStoreMode : byte
    {
        InMemoryRepository = 1,
        InMemoryLog = 2,
        PersistantRepository = 3,
        PersistantLog = 4
    }
}
