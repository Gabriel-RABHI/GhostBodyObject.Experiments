namespace GhostBodyObject.Common.Objects
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class EntityPropertyAttribute : Attribute
    {
        public ushort Id { get; }

        public EntityPropertyAttribute(ushort id)
        {
            Id = id;
        }
    }
}
