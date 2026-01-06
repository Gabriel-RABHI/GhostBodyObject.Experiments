namespace GhostBodyObject.Repository.Ghost.Constants
{
    public enum GhostIdKind : byte
    {
        Entity = 0x00,
        ForwardLink = 0x01,
        BackwardLink = 0x02,
        Edge = 0x03,
        QueueEntry = 0x04,
        MapEntry = 0x05,
        System = 0x06,
        Other = 0x07
    }
}
