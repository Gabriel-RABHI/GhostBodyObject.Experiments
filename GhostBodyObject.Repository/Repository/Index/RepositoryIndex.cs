using GhostBodyObject.Repository.Ghost.Constants;
using GhostBodyObject.Repository.Ghost.Structs;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GhostBodyObject.Repository.Repository.Index
{
    internal unsafe class RepositoryIndex
    {
        public delegate*<Customer, Memory<byte>, int, void> SwapAnyArray;


        private RepositorySingleTypeIndex[] _maps;
        
        public RepositoryIndex()
        {
            _maps = new RepositorySingleTypeIndex[GhostId.MAX_TYPE_ID];
        }

        public 
    }

    internal class RepositorySingleTypeIndex
    {
        private RepositorySingleKindIndex[] _byKind;

        public RepositorySingleTypeIndex()
        {
            _byKind = new RepositorySingleKindIndex[GhostId.MAX_KIND];
        }
    }

    internal class RepositorySingleKindIndex
    {
        private long _minTxnId;
        private long _maxTxnId;

        public RepositorySingleTypeIndex()
        {
            _byKind = new FastTransactionnalEntryMap[GhostKind.MAX_KIND_ID];
            for (int i = 0; i < GhostKind.MAX_KIND_ID; i++)
            {
                _byKind[i] = new FastTransactionnalEntryMap();
            }
        }
    }
}
