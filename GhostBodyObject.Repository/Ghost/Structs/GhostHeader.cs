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

using GhostBodyObject.Repository.Ghost.Constants;
using System.Runtime.InteropServices;

namespace GhostBodyObject.Repository.Ghost.Structs
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 40)]
    public unsafe struct GhostHeader
    {
        public const int SIZE = 40;
        public const int WHITE_OFFSET = 32;

        // ---------------------------------------------------------
        // Standard Fields
        // ---------------------------------------------------------
        [FieldOffset(0)]
        public GhostId Id;

        [FieldOffset(16)]
        public long TxnId;

        [FieldOffset(24)]
        public ushort ModelVersion;

        [FieldOffset(26)]
        public GhostStatus Status;

        [FieldOffset(27)]
        public byte Flags;

        [FieldOffset(28)]
        public int MutationCounter;

        // ---------------------------------------------------------
        // Zero Fields
        // ---------------------------------------------------------
        [FieldOffset(32)]
        public long White;

        public void Initialize(ushort modelVersion)
        {
            Id = default;
            TxnId = 0;
            White = 0;
            ModelVersion = modelVersion;
            Status = GhostStatus.Inserted;
            Flags = 0x00;
            MutationCounter = 0;
        }
    }
}
