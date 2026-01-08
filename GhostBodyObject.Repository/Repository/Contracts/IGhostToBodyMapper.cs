using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;

namespace GhostBodyObject.Repository.Repository.Contracts
{
    public unsafe interface IGhostToBodyMapper<TBody>
        where TBody : BodyBase
    {
        TBody CreateBody(GhostHeader* header);
    }
}
