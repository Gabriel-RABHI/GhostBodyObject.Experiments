using GhostBodyObject.Repository.Body.Contracts;

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
