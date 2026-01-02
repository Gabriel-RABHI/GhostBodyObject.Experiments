using System;
using System.Collections.Generic;
using System.Text;

namespace GhostBodyObject.Repository.Model.Schema
{
    internal class PropertyDefinition
    {
        public string GenerateCode()
        {
            return string.Empty;
        }
    }

    internal class PropertyIndexDefinition
    {
        public string GenerateCode()
        {
            return string.Empty;
        }
    }

    internal class EntitySchemaLayout
    {
        public IEnumerable<PropertyDefinition> PropertyDefinitions { get; }

        public IEnumerable<PropertyIndexDefinition> PropertyIndexDefinitions { get; }

        public string GenerateCode()
        {
            return string.Empty;
        }
    }

    internal class EntitySchemaHierarchy
    {
        public IEnumerable<EntitySchemaLayout> LayoutVersions { get; }

        public string GenerateCode()
        {
            return string.Empty;
        }
    }

    internal class RepositorySchemaRegistry
    {
        public IEnumerable<EntitySchemaHierarchy> Entities { get; }

        public string GenerateCode()
        {
            return string.Empty;
        }
    }
}
