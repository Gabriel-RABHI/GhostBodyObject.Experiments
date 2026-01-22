/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

using GhostBodyObject.Common.Memory;
using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Body.Vectors
{
    public unsafe static class VectorTableRegistry<TRepository, TBody>
        where TRepository : GhostRepositoryBase
        where TBody : BodyBase
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
                .SelectMany(assembly => {
                    try
                    {
                        // Skip dynamic assemblies or those that throw on GetTypes
                        if (assembly.IsDynamic) return Type.EmptyTypes;
                        return assembly.GetTypes();
                    } catch
                    {
                        // Safely ignore assemblies that fail to enumerate types
                        return Type.EmptyTypes;
                    }
                })
                .Where(t => t.IsClass && t.IsSealed && t.IsAbstract) // Must be a static class
                .Select(t => new {
                    Type = t,
                    // Look for the required static properties and method
                    RepoProp = t.GetProperty("RepositoryType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    BodyProp = t.GetProperty("BodyType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    VersionProp = t.GetProperty("TargetVersion", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static),
                    Method = t.GetMethod("GetTableRecord", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                })
                // Filter: Must have all members
                .Where(x => x.RepoProp != null && x.BodyProp != null && x.VersionProp != null && x.Method != null)
                // Filter: Must match current TRepository and TBody
                .Where(x => {
                    var rType = x.RepoProp.GetValue(null) as Type;
                    var bType = x.BodyProp.GetValue(null) as Type;
                    return rType == typeof(TRepository) && bType == typeof(TBody);
                })
                .Select(x => new {
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

        static public void BuildStandaloneVersion(PinnedMemory<byte> ghost, TBody body)
        {
            body._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].Standalone;
            body._data = ghost;
        }

        static public void BuildStandaloneVersion(int version, TBody body)
        {
            body._vTablePtr = (nint)_versionToTable[version - 1].Standalone;
            body._data = TransientGhostMemoryAllocator.Allocate(body._vTableHeader->MinimalGhostSize);
            var ghost = &_versionToTable[version - 1].InitialGhost;
            ghost->CopyTo(body._data);
            body._data.Set<GhostId>(0, GhostId.NewId(GhostIdKind.Entity, ghost->As<GhostId>()->TypeIdentifier));
        }

        static public void BuildMappedVersion(PinnedMemory<byte> ghost, TBody body, bool readOnly)
        {
            if (readOnly)
                body._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].MappedReadOnly;
            else
                body._vTablePtr = (nint)_versionToTable[ghost.Get<GhostHeader>().ModelVersion - 1].MappedMutable;
            body._data = ghost;
        }

        static public void MappedToStandaloneVersion(TBody body, GhostStatus newStatus)
        {
            var v = body._vTableHeader->ModelVersion;
            var g = body._data;
            body._vTablePtr = (nint)_versionToTable[v - 1].Standalone;
            body._data = TransientGhostMemoryAllocator.Allocate(g.Length);
            g.CopyTo(body._data);
            body.Header->Status = newStatus;
        }
    }
}
