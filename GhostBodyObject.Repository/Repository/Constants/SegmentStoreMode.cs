namespace GhostBodyObject.Repository.Repository.Constants
{
    public enum SegmentStoreMode : byte
    {
        InMemoryRepository = 1,
        InMemoryLog = 2,
        // -------- Add in virtual memory volatile to makes data dumped on disk ?
        PersistantRepository = 3,
        PersistantLog = 4
    }
}
