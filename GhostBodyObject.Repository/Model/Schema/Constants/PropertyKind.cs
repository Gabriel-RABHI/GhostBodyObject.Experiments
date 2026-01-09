namespace GhostBodyObject.Repository.Model.Schema.Constants
{
    public enum PropertyKind
    {
        Value,
        ArrayOfValues,
        WeakBodyReference, // Simply a GhostId, used as an Handle (resolved dynamically)
        BodyReference, // Strong reference to another BodyObject, stored as GhostId in the relation table
        WeakBodyCollection,
        BodyCollection,
    }
}
