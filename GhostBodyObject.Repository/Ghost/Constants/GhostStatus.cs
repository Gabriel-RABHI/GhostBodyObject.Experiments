namespace GhostBodyObject.Repository.Ghost.Constants
{
    public enum GhostStatus : byte
    {
        Inserted = 0x00,
        Mapped = 0x02,
        MappedModified = 0x04,
        MappedDeleted = 0x06
    }
}
