using System.Runtime.InteropServices.Marshalling;

namespace GhostBodyObject.CodeGenerator
{
    /*
     * 1) Model interface => ModelParser => ModelDescriptor (factual model representation)
     * 
     * 2) ModelDescriptor => ModelComponents.BuildCodeTree (plug-in architecture) -> CodeTree (technical code representation)
     * 
     * 3) CodeTree.Render => C# Source Files
     * 
     * 
     * CodeTree
     * --------
     * 
     * It is a tree of node that generate C# code by a recursive call with a CodeGenerationContext instance. 
     * 
     * FileNode (generate a .cs file with usings)
     *     |
     *     NamespaceNode (generate a namespace with brackets)
     *         |
     *         BodyClassNode (generate a class with brackets, standard fields, constants ...)
     *         |   |
     *         |   BodyPropertyNode (generate a property, standard get/set, etc.)
     *         |
     *         |
     *         InterfaceNode
     *         |   |
     *         |   InterfacePropertyNode (generate a property, standard get/set, etc.)
     *         |   
     *         |
     *         RepositoryClassNode (generate a class / interface with brackets, standard fields, constants ...)
     *         |   |
     *         |   RepositoryPropertyNode (generate a property, standard get/set, etc.)
     *         |   
     *         |
     *         TransactionClassNode (generate standard methods ...)
     *             |
     *             BodyCollectionPropertyNode
     *             
     *  
     * There is a set of standard Node components to generate the common code structures (FileNode, NamespaceNode, BodyClassNode, BodyPropertyNode, etc.)
     * 
     * There is a set of interfaces to identify each general type of Node : all Nodes class know this contracts.
     *
     * There is two phases :
     * - void Prepare(GenerationContext ctx);
     * - void Render(CodeWriter writer, GenerationContext ctx);
     * 
     *      public interface ICodeNode
            {
                void Prepare(GenerationContext ctx);
    
                void Render(CodeWriter writer, GenerationContext ctx);
            }
     * 
     */


    public class ModelDescriptor
    {
        // -------- FIXED PROPERTIES -------- //
        // Unique identifier for the repository in the model, fixed for the software lifetime
        public ushort RepositoryIdentifier { get; }

        // -------- VARIABLE -------- //
        // List of repository versions in the model, where name or entities list can differ
        public List<RepositoryDescriptor> RepositoryVersions { get; }
    }

    public class RepositoryDescriptor
    {
        // -------- VARIABLE -------- //
        public int Version { get; }

        public string RepositoryName { get; }

        public List<EntityDescriptor> Entities { get; }
    }

    public class EntityDescriptor
    {
        // -------- FIXED PROPERTIES -------- //
        // Type identifier used in GhostId.TypeCombo, fixed for the entity across versions / software lifetime
        public ushort TypeIdentifier { get; }

        // -------- VARIABLE -------- //
        public List<EntityVersionDescriptor> Versions { get; }
    }

    public class EntityVersionDescriptor
    {
        public int Version { get; }

        public string EntityName { get; }

        public List<PropertyDescriptor> Properties { get; }

        // -------- IMPLICIT
        // This permit to the add the properties of the parent entity, and having the generated interface
        // MTenantEntity => TenantEntity + ITenantEntity
        // MUser (with MTenantEntity) => User : ITenantEntity + IUser : ITenantEntity
        public List<EntityInheritanceDescriptor> Inheritences { get; }

        public List<AttributeDescriptor> Attributes { get; }
    }

    public class EntityInheritanceDescriptor
    {
        public int TypeIdentifier { get; }

        public int Version { get; }
    }


    public enum PropertyKind
    {
        Value,
        ArrayOfValues,
        WeakBodyReference, // Simply a GhostId, used as an Handle (resolved dynamically)
        BodyReference, // Strong reference to another BodyObject, stored as GhostId in the relation table
        WeakBodyCollection,
        BodyCollection,
    }

    public enum PropertyRelationStoreSpace
    {
        InGhost,
        InDedicatedGhost,
        AsEdge,
    }

    public enum PropertyRelationCardinality
    {
        _1_1,
        _1_N,
        _N_1,
        _N_N
    }

    public enum PropertyRelationAutomation
    {
        None,
        CascadeDeleteTarget,
        CascadingDeleteSource,
        DeleteTargetOrphan,
        DeleteSourceOrphan
    }

    public enum PropertyRelationContraint
    {
        None,
        ForbidTargetDeletion,
        ForbidSourceDeletion,
        RequiredForCommit
    }

    public class PropertyDescriptor
    {
        // -------- FIXED PROPERTIES -------- //
        // Unique identifier for the property in the entity, fixed for the property across versions / software lifetime
        public int PropertyIdentifier { get; }

        // Type name of the property (e.g., "System.Int32", "GhostBodyObject.Repository.Ghost.Structs.GhostId", etc.)
        // Is fixed for any PropertyIdentifier - to change the type, a new PropertyIdentifier must be created
        public string PropertyBaseType { get; }

        public PropertyKind PropertyType { get; }

        public PropertyRelationStoreSpace ForwardStoreSpace { get; }

        public PropertyRelationStoreSpace BackwardStoreSpace { get; }

        public ushort RelationIdentifier { get; }

        public string RelationName { get; }

        // -------- VARIABLE PROPERTIES -------- //
        // Name of the property in the entity class, can change across versions
        public string PropertyName { get; }

        // Is the setter private ?
        public bool ReadOnly { get; }

        // Is completly private ?
        public bool Private { get; }

        public List<AttributeDescriptor> Attributes { get; }

        public List<PropertyIndexDescriptor> Indexes { get; }
    }

    public class AttributeDescriptor
    {
        public string AttributeName { get; }

        public List<AttributeParameterDescriptor> AttributeParameters { get; }
    }

    public class AttributeParameterDescriptor
    {
        public string Key { get; }

        public string Value { get; }
    }

    public class PropertyIndexDescriptor
    {
        // -------- FIXED PROPERTIES -------- //
        // 8 by 8 : 0..8..16..24..  up to 8000 (1000 indexes max) - it define the index Entities .TypeIdentifier
        public ushort IndexIdentifier { get; }

        // Define the type of the index : SkipList / MultiLinkedList / Bitmap / BasicTextual / etc.
        public string IndexTypeName { get; }

        // -------- VARIABLE PROPERTIES -------- //
        public string IndexName { get; }

        public bool Private { get; }

        public bool Composite { get; }

        public int ComponentOrder { get; }
    }
}
