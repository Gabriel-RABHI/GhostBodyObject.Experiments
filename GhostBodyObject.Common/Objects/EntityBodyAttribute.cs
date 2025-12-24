namespace GhostBodyObject.Common.Objects
{
    [AttributeUsage(AttributeTargets.Interface, Inherited = true)]
    public class EntityBodyAttribute : Attribute
    {
        public ushort TypeCode { get; }

        public int Version { get; }

        public ushort LastPropertyId { get; }

        public EntityBodyAttribute(ushort typeCode, int version, ushort lastPropertyId)
        {
            TypeCode = typeCode;
            Version = version;
            LastPropertyId = lastPropertyId;
        }
    }

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
