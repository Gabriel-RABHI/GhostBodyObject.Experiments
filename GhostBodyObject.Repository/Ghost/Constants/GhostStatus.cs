namespace GhostBodyObject.Repository.Ghost.Constants
{
    public enum GhostStatus : byte
    {
        Standalone = 0x00,
        Mapped = 0x02,
        MappedDeleted = 0x04
    }
}
