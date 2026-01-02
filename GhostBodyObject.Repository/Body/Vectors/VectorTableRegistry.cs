using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GhostBodyObject.HandWritten.BloggerApp.Entities.User
{
    public unsafe static class VectorTableRegistry<TRepository, TBody>
        where TRepository : GhostRepository
        where TBody : IEntityBody
    {
        private static VectorTableRecord* _versionToTable;
        private static int _topVersion = 1;

        public static int TopVersion => _topVersion;

        static VectorTableRegistry()
        {
            if (_versionToTable == null)
                UpdateRegistry();
        }

        public static unsafe void UpdateRegistry()
        {
            // -------- 1. Search for all compatible Builder classes in the AppDomain
            var validBuilders = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        // Skip dynamic assemblies or those that throw on GetTypes
                        if (assembly.IsDynamic) return Type.EmptyTypes;
                        return assembly.GetTypes();
                    }
                    catch
                    {
                        // Safely ignore assemblies that fail to enumerate types
                        return Type.EmptyTypes;
                    }
                })
                .Where(t => t.IsClass && t.IsSealed && t.IsAbstract) // Must be a static class
                .Select(t => new
                {
                    Type = t,
                    // Look for the required static properties and method
                    RepoProp = t.GetProperty("RepositoryType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    BodyProp = t.GetProperty("BodyType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    VersionProp = t.GetProperty("SourceVersion", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    Method = t.GetMethod("GetTableRecord", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                })
                // Filter: Must have all members
                .Where(x => x.RepoProp != null && x.BodyProp != null && x.VersionProp != null && x.Method != null)
                // Filter: Must match current TRepository and TBody
                .Where(x =>
                {
                    var rType = x.RepoProp.GetValue(null) as Type;
                    var bType = x.BodyProp.GetValue(null) as Type;
                    return rType == typeof(TRepository) && bType == typeof(TBody);
                })
                .Select(x => new
                {
                    // The SourceVersion determines the Index in the table
                    Version = (int)x.VersionProp.GetValue(null),
                    Method = x.Method
                })
                .ToList();

            if (validBuilders.Count == 0) return;

            // -------- 2. Determine if we need to expand the array
            int maxFoundVersion = validBuilders.Max(x => x.Version);

            // Ensure _topVersion covers the new max (always growing)
            if (maxFoundVersion > _topVersion)
            {
                _topVersion = maxFoundVersion;
            }

            // -------- 3. Reallocate unmanaged memory to fit the new size
            // NativeMemory.Realloc handles NULL input (acts as Alloc) and copies data if moving
            nuint newSize = (nuint)(sizeof(VectorTableRecord) * _topVersion);
            void* newPtr = NativeMemory.Realloc(_versionToTable, newSize);
            _versionToTable = (VectorTableRecord*)newPtr;

            // -------- 4. Populate the table
            foreach (var builder in validBuilders)
            {
                // Invoke GetTableRecord()
                var record = (VectorTableRecord)builder.Method.Invoke(null, null);

                // Map Version to Index (1-based version -> 0-based index)
                int index = builder.Version - 1;

                // Write to unmanaged memory
                _versionToTable[index] = record;
            }
        }

        static public void BuildInitialVersion(int version, TBody body)
        {
            var union = Unsafe.As<BodyUnion>(body);
            union._vTablePtr = (nint)_versionToTable[version - 1].Initial;
            union._data = _versionToTable[version - 1].InitialGhost;
        }

        static public void BuildStandaloneVersion(PinnedMemory<byte> ghost, TBody body)
        {
            var union = Unsafe.As<BodyUnion>(body);
            union._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].Standalone;
            union._data = ghost;
        }

        static public void BuildMappedVersion(PinnedMemory<byte> ghost, TBody body, bool readOnly)
        {
            var union = Unsafe.As<BodyUnion>(body);
            if (readOnly)
                union._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].MappedReadOnly;
            else
                union._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].MappedMutable;
            union._data = ghost;
        }
    }
}
