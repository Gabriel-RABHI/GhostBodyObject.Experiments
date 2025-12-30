using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Structs;

namespace GhostBodyObject.Repository.Repository.Contracts
{
    public unsafe interface ISegmentStore
    {
        GhostHeader* ToGhostHeaderPointer(SegmentReference reference);
    }
}
