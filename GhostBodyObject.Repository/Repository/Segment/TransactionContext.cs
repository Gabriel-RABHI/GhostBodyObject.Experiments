using GhostBodyObject.Repository.Body.Contracts;
using GhostBodyObject.Repository.Ghost.Structs;
using GhostBodyObject.Repository.Repository.Structs;
using System;
using System.Collections.Generic;

namespace GhostBodyObject.Repository.Repository.Segment
{
    public class TransactionContext
    {
        public int StartSegmentId;
        public int StartOffset;
        public bool IsSplit;
        
        public int CurrentSegmentId;
        public int CurrentOffset;
        
        public List<BodyBase> Bodies;
        public (int SegmentId, int Offset)[] BodyLocations;
        
        public int EndSegmentId;
        public int EndOffset;
        
        public long TransactionId;
    }
}
